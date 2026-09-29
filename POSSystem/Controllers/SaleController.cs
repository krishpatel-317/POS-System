using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    public class SaleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SaleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ (List all sales)
        public IActionResult Index()
        {
            var sales = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .Include(s => s.Payment)
                .OrderByDescending(s => s.SaleDate)
                .ToList();
            
            return View(sales);
        }

        // 2. DETAILS (Show specific sale with receipt items)
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
            ViewBag.Customers = new SelectList(_context.Customers.OrderBy(c => c.Name).ToList(), "CustomerId", "Name");
            
            // Staff members with role names
            var usersWithRoles = _context.Users
                .Include(u => u.Role)
                .OrderBy(u => u.Name)
                .Select(u => new {
                    u.UserId,
                    DisplayName = u.Name + (u.Role != null ? " (" + u.Role.Name + ")" : "")
                })
                .ToList();

            ViewBag.Users = new SelectList(usersWithRoles, "UserId", "DisplayName");

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
        public IActionResult Create(int? customerId, int userId, string paymentMethod, List<int> productIds, List<int> quantities, List<decimal> unitPrices)
        {
            // Ensure cart is not empty
            if (productIds == null || productIds.Count == 0)
            {
                TempData["Error"] = "Please add at least one product to the cart before checking out.";
                return RedirectToAction("Create");
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
                UserId = userId
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

            TempData["Success"] = $"Sale #{sale.SaleId} recorded with {productIds.Count} line item(s)! Inventory stock updated automatically.";
            return RedirectToAction("Index");
        }
    }
}
