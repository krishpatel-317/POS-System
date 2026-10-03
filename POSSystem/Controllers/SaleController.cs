using Microsoft.AspNetCore.Authorization;
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

        public SaleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper: Ensure the logged-in Identity user exists in the POSUsers table for cashier tracking
        private User GetOrCreateCurrentPOSUser()
        {
            string username = User.Identity?.Name ?? "User";
            var posUser = _context.POSUsers.Include(u => u.Role)
                .FirstOrDefault(u => u.Username == username || u.Name == username);

            if (posUser == null)
            {
                // Ensure default Cashier role exists
                var defaultRole = _context.POSRoles.FirstOrDefault(r => r.Name == "Cashier") 
                                  ?? _context.POSRoles.FirstOrDefault();
                
                if (defaultRole == null)
                {
                    defaultRole = new Role { Name = "Cashier" };
                    _context.POSRoles.Add(defaultRole);
                    _context.SaveChanges();
                }

                posUser = new User
                {
                    Name = username,
                    Username = username,
                    Password = "IdentityManaged", // Auth handled by ASP.NET Core Identity
                    RoleId = defaultRole.RoleId
                };

                _context.POSUsers.Add(posUser);
                _context.SaveChanges();
            }

            return posUser;
        }

        // 1. READ & FILTER (Defaults to showing only the logged-in user's sales, with option to view all store sales)
        public IActionResult Index(int? cashierId, string? paymentMethod, string? dateRange, string? search, bool showAllStoreSales = false)
        {
            var currentPosUser = GetOrCreateCurrentPOSUser();

            // Strict Privilege Isolation: Cashiers can NEVER see other staff's sales
            bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Manager");
            if (!isPrivileged)
            {
                cashierId = currentPosUser.UserId;
                showAllStoreSales = false;
            }
            else if (!showAllStoreSales && (!cashierId.HasValue || cashierId.Value == 0))
            {
                // Admins/Managers default to their own sales, but can toggle to all store sales
                cashierId = currentPosUser.UserId;
            }

            IQueryable<Sale> query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Payment)
                .Include(s => s.SaleItems)
                .AsQueryable();

            // Filter by Cashier if specified (or default to current user)
            if (cashierId.HasValue && cashierId.Value > 0)
            {
                query = query.Where(s => s.UserId == cashierId.Value);
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

            // Search by Customer Name or Sale #
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(s => 
                    (s.Customer != null && s.Customer.Name.Contains(search)) ||
                    (s.User != null && s.User.Name.Contains(search)) ||
                    s.SaleId.ToString().Contains(search));
            }

            var sales = query.OrderByDescending(s => s.SaleDate).ToList();

            // Filter dropdown data
            ViewBag.Cashiers = new SelectList(_context.POSUsers.OrderBy(u => u.Name).ToList(), "UserId", "Name", cashierId);
            ViewBag.PaymentMethods = new SelectList(Enum.GetNames<PaymentMethodType>(), paymentMethod);
            
            // Pass active filter state to view
            ViewBag.SelectedCashier = cashierId;
            ViewBag.SelectedPaymentMethod = paymentMethod;
            ViewBag.SelectedDateRange = dateRange;
            ViewBag.SearchTerm = search;
            ViewBag.ShowAllStoreSales = showAllStoreSales;
            ViewBag.CurrentPosUserId = currentPosUser.UserId;
            ViewBag.CurrentUserName = currentPosUser.Name;

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
            var currentPosUser = GetOrCreateCurrentPOSUser();

            ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name).ToList(), "CustomerId", "Name");
            
            // Staff members with role names, default selected to current logged-in user
            var usersWithRoles = _context.POSUsers
                .Include(u => u.Role)
                .OrderBy(u => u.Name)
                .Select(u => new {
                    u.UserId,
                    DisplayName = u.Name + (u.Role != null ? " (" + u.Role.Name + ")" : "")
                })
                .ToList();

            ViewBag.Users = new SelectList(usersWithRoles, "UserId", "DisplayName", currentPosUser.UserId);
            ViewBag.CurrentCashierId = currentPosUser.UserId;
            ViewBag.CurrentCashierName = currentPosUser.Name;

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
        public IActionResult Create(int? customerId, int? userId, string paymentMethod, List<int> productIds, List<int> quantities, List<decimal> unitPrices)
        {
            // Ensure cart is not empty
            if (productIds == null || productIds.Count == 0)
            {
                TempData["Error"] = "Please add at least one product to the cart before checking out.";
                return RedirectToAction("Create");
            }

            // Fallback to current logged-in user if userId wasn't supplied
            if (!userId.HasValue || userId.Value == 0)
            {
                var currentUser = GetOrCreateCurrentPOSUser();
                userId = currentUser.UserId;
            }

            // Calculate total bill amount
            decimal grandTotal = 0;
            for (int i = 0; i < productIds.Count; i++)
            {
                grandTotal += quantities[i] * unitPrices[i];
            }

            // 1. Save Sale Header
            var sale = new Sale
            {
                SaleDate = DateTime.Now,
                TotalAmount = grandTotal,
                CustomerId = customerId == 0 ? null : customerId, // Walk-in support
                UserId = userId.Value
            };

            _context.Sales.Add(sale);
            _context.SaveChanges(); // Generates SaleId

            // 2. Save SaleItems & Deduct Stock from Database
            for (int i = 0; i < productIds.Count; i++)
            {
                int pId = productIds[i];
                int qty = quantities[i];
                decimal price = unitPrices[i];

                var saleItem = new SaleItem
                {
                    SaleId = sale.SaleId,
                    ProductId = pId,
                    Quantity = qty,
                    UnitPrice = price,
                    TotalPrice = qty * price
                };
                _context.SaleItems.Add(saleItem);

                // Reduce inventory stock in Product table
                var product = _context.Products.Find(pId);
                if (product != null)
                {
                    product.StockQuantity -= qty;
                    if (product.StockQuantity < 0) product.StockQuantity = 0;
                }
            }

            // 3. Save Payment Record
            var payment = new Payment
            {
                Amount = grandTotal,
                PaymentMethod = paymentMethod ?? PaymentMethodType.Cash.ToString(),
                PaymentDate = DateTime.Now,
                SaleId = sale.SaleId
            };
            _context.Payments.Add(payment);

            _context.SaveChanges();

            TempData["Success"] = $"Sale #{sale.SaleId} recorded under your account! Billed ₹{grandTotal:N2}.";
            return RedirectToAction("Details", new { id = sale.SaleId });
        }
    }
}
