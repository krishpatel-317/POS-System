using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class SaleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SaleController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. READ & FILTER (Defaults to showing only the logged-in user's sales, with option to view all store sales)
        public IActionResult Index(string? cashierId, string? paymentMethod, string? dateRange, string? search, bool showAllStoreSales = false)
        {
            string currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            string currentUserName = User.Identity?.Name ?? "User";

            // Strict Privilege Isolation: Cashiers can NEVER see other staff's sales
            bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Manager");
            if (!isPrivileged)
            {
                cashierId = currentUserId;
                showAllStoreSales = false;
            }
            else if (!showAllStoreSales && string.IsNullOrEmpty(cashierId))
            {
                // Admins/Managers default to their own sales, but can toggle to all store sales
                cashierId = currentUserId;
            }

            IQueryable<Sale> query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Payment)
                .Include(s => s.SaleItems)
                .AsQueryable();

            // Filter by Cashier if specified (or default to current user)
            if (!string.IsNullOrEmpty(cashierId))
            {
                query = query.Where(s => s.UserId == cashierId);
            }

            // Filter by Payment Method
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                query = query.Where(s => s.Payment != null && s.Payment.PaymentMethod == paymentMethod);
            }

            // Filter by Date Range
            DateTime today = DateTime.Today;
            if (dateRange == "today")
            {
                query = query.Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1));
            }
            else if (dateRange == "week")
            {
                DateTime startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                query = query.Where(s => s.SaleDate >= startOfWeek);
            }
            else if (dateRange == "month")
            {
                DateTime startOfMonth = new DateTime(today.Year, today.Month, 1);
                query = query.Where(s => s.SaleDate >= startOfMonth);
            }

            // Search by Customer Name, Cashier Username, or Sale #
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(s => 
                    (s.Customer != null && s.Customer.Name.Contains(search)) ||
                    (s.User != null && s.User.UserName != null && s.User.UserName.Contains(search)) ||
                    s.SaleId.ToString().Contains(search));
            }

            var sales = query.OrderByDescending(s => s.SaleDate).ToList();

            // Filter dropdown data
            var staffUsers = _userManager.Users.OrderBy(u => u.UserName).ToList();
            ViewBag.Cashiers = new SelectList(staffUsers, "Id", "UserName", cashierId);
            ViewBag.PaymentMethods = new SelectList(Enum.GetNames<PaymentMethodType>(), paymentMethod);
            
            // Pass active filter state to view
            ViewBag.SelectedCashier = cashierId;
            ViewBag.SelectedPaymentMethod = paymentMethod;
            ViewBag.SelectedDateRange = dateRange;
            ViewBag.SearchTerm = search;
            ViewBag.ShowAllStoreSales = showAllStoreSales;
            ViewBag.CurrentPosUserId = currentUserId;
            ViewBag.CurrentUserName = currentUserName;

            // Summary metrics for current filtered view
            ViewBag.FilteredRevenue = sales.Sum(s => s.TotalAmount);
            ViewBag.FilteredCount = sales.Count;
            ViewBag.FilteredAvgTicket = sales.Count > 0 ? (sales.Sum(s => s.TotalAmount) / sales.Count) : 0;

            return View(sales);
        }

        // 2. DETAILS (Show specific sale with printable receipt)
        public IActionResult Details(int id)
        {
            var sale = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Payment)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.Product)
                .FirstOrDefault(s => s.SaleId == id);

            if (sale == null)
            {
                return NotFound();
            }

            return View(sale);
        }

        // 3. CREATE (Show POS Cart Interface)
        public IActionResult Create()
        {
            string currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            string currentUserName = User.Identity?.Name ?? "User";

            ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name).ToList(), "CustomerId", "Name");
            ViewBag.CurrentCashierId = currentUserId;
            ViewBag.CurrentCashierName = currentUserName;

            // Products available in stock
            ViewBag.AvailableProducts = _context.Products
                .Where(p => p.StockQuantity > 0)
                .OrderBy(p => p.Name)
                .ToList();

            // Payment methods populated from PaymentMethodType Enum
            ViewBag.PaymentMethods = new SelectList(Enum.GetNames<PaymentMethodType>());
            
            return View();
        }

        // 4. CREATE (Process Cart Checkout, Create Sale & Items, Update Stock)
        [HttpPost]
        public IActionResult Create(int? customerId, string? userId, string paymentMethod, List<int> productIds, List<int> quantities, List<decimal> unitPrices)
        {
            // Ensure cart is not empty
            if (productIds == null || productIds.Count == 0)
            {
                TempData["Error"] = "Please add at least one product to the cart before checking out.";
                return RedirectToAction("Create");
            }

            // Cashier is always the currently logged-in Identity user
            string currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            if (string.IsNullOrEmpty(currentUserId))
            {
                var user = _userManager.FindByNameAsync(User.Identity?.Name ?? "").GetAwaiter().GetResult();
                currentUserId = user?.Id ?? string.Empty;
            }

            // 1. Group duplicate cart line items to validate total requested quantities accurately
            var consolidatedItems = new Dictionary<int, (int Quantity, decimal UnitPrice)>();
            for (int i = 0; i < productIds.Count; i++)
            {
                int pId = productIds[i];
                int qty = quantities[i];
                decimal price = unitPrices[i];

                if (qty <= 0) continue; // Ignore non-positive quantities

                if (consolidatedItems.ContainsKey(pId))
                {
                    var existing = consolidatedItems[pId];
                    consolidatedItems[pId] = (existing.Quantity + qty, price);
                }
                else
                {
                    consolidatedItems[pId] = (qty, price);
                }
            }

            if (consolidatedItems.Count == 0)
            {
                TempData["Error"] = "Cart contains no valid quantities.";
                return RedirectToAction("Create");
            }

            // 2. Validate inventory stock for every product
            foreach (var item in consolidatedItems)
            {
                var product = _context.Products.Find(item.Key);
                if (product == null)
                {
                    TempData["Error"] = "One of the selected products was not found in the catalog.";
                    return RedirectToAction("Create");
                }
                if (product.StockQuantity < item.Value.Quantity)
                {
                    TempData["Error"] = $"Cannot complete sale: '{product.Name}' has insufficient stock (Available: {product.StockQuantity}, Requested: {item.Value.Quantity}).";
                    return RedirectToAction("Create");
                }
            }

            // 3. Validate payment method against PaymentMethodType enum
            string validatedPaymentMethod = PaymentMethodType.Cash.ToString();
            if (!string.IsNullOrWhiteSpace(paymentMethod) && Enum.TryParse<PaymentMethodType>(paymentMethod, true, out var parsedMethod))
            {
                validatedPaymentMethod = parsedMethod.ToString();
            }

            // 4. Calculate total amount & build Sale with child SaleItems
            decimal grandTotal = 0;
            var sale = new Sale
            {
                SaleDate = DateTime.Now,
                CustomerId = customerId == 0 ? null : customerId,
                UserId = currentUserId,
                SaleItems = new List<SaleItem>()
            };

            foreach (var item in consolidatedItems)
            {
                int pId = item.Key;
                int qty = item.Value.Quantity;
                decimal price = item.Value.UnitPrice;
                decimal subtotal = qty * price;
                grandTotal += subtotal;

                sale.SaleItems.Add(new SaleItem
                {
                    ProductId = pId,
                    Quantity = qty,
                    UnitPrice = price,
                    TotalPrice = subtotal
                });

                // Deduct inventory stock
                var product = _context.Products.Find(pId);
                if (product != null)
                {
                    product.StockQuantity -= qty;
                }
            }

            sale.TotalAmount = grandTotal;

            // 5. Attach Payment record
            sale.Payment = new Payment
            {
                Amount = grandTotal,
                PaymentMethod = validatedPaymentMethod,
                PaymentDate = DateTime.Now
            };

            // 6. Save entire graph in a single atomic database commit
            _context.Sales.Add(sale);
            _context.SaveChanges();

            TempData["Success"] = $"Sale #{sale.SaleId} recorded successfully! Billed ₹{grandTotal:N2}.";
            return RedirectToAction("Details", new { id = sale.SaleId });
        }
    }
}
