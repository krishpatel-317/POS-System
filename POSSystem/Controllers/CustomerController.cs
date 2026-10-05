using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ & SEARCH (List all customers with purchase stats and search filter)
        public IActionResult Index(string? search)
        {
            var query = _context.Customers
                .Include(c => c.Sales)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(c => c.Name.Contains(search) || c.Phone.Contains(search) || c.Email.Contains(search));
            }

            var customers = query.OrderBy(c => c.Name).ToList();
            ViewBag.Search = search;
            ViewBag.TotalCustomersCount = _context.Customers.Count();
            ViewBag.ActiveBuyersCount = _context.Customers.Count(c => c.Sales.Any(s => s.Status != "Voided"));
            return View(customers);
        }

        // 2. DETAILS (Customer profile and complete order history)
        public IActionResult Details(int id)
        {
            var customer = _context.Customers
                .Include(c => c.Sales.OrderByDescending(s => s.SaleDate))
                    .ThenInclude(s => s.Payment)
                .Include(c => c.Sales)
                    .ThenInclude(s => s.SaleItems)
                .FirstOrDefault(c => c.CustomerId == id);

            if (customer == null) return NotFound();

            var activeSales = customer.Sales.Where(s => s.Status != "Voided").ToList();
            ViewBag.TotalSpent = activeSales.Sum(s => s.TotalAmount);
            ViewBag.TotalOrders = activeSales.Count;
            ViewBag.AverageTicket = activeSales.Count > 0 ? (activeSales.Sum(s => s.TotalAmount) / activeSales.Count) : 0m;

            return View(customer);
        }

        // 3. QUICK CREATE (AJAX on-the-fly customer registration from POS billing counter)
        [HttpPost]
        public IActionResult QuickCreate(string name, string phone, string? email)
        {
            string trimmedName = name?.Trim() ?? string.Empty;
            string trimmedPhone = phone?.Trim() ?? string.Empty;
            string trimmedEmail = email?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return Json(new { success = false, message = "Customer name is required." });
            }
            if (string.IsNullOrWhiteSpace(trimmedPhone))
            {
                return Json(new { success = false, message = "Phone number is required." });
            }

            // Duplicate checks
            if (_context.Customers.Any(c => c.Phone.ToLower() == trimmedPhone.ToLower()))
            {
                return Json(new { success = false, message = $"A customer with phone '{trimmedPhone}' already exists." });
            }
            if (!string.IsNullOrEmpty(trimmedEmail) && _context.Customers.Any(c => c.Email.ToLower() == trimmedEmail.ToLower()))
            {
                return Json(new { success = false, message = $"A customer with email '{trimmedEmail}' already exists." });
            }

            var customer = new Customer
            {
                Name = trimmedName,
                Phone = trimmedPhone,
                Email = trimmedEmail
            };

            _context.Customers.Add(customer);
            _context.SaveChanges();

            return Json(new
            {
                success = true,
                customerId = customer.CustomerId,
                name = customer.Name,
                phone = customer.Phone,
                displayName = $"{customer.Name} ({customer.Phone})"
            });
        }

        // 4. CREATE (Show Form - all authenticated staff)
        public IActionResult Create()
        {
            return View();
        }

        // 3. CREATE (Save to Database - all authenticated staff)
        [HttpPost]
        public IActionResult Create(Customer customer)
        {
            ModelState.Remove("Sales"); // Not provided in the form

            string trimmedName = customer.Name?.Trim() ?? string.Empty;
            string trimmedPhone = customer.Phone?.Trim() ?? string.Empty;
            string trimmedEmail = customer.Email?.Trim() ?? string.Empty;

            // 1. Phone number duplicate validation
            if (!string.IsNullOrEmpty(trimmedPhone))
            {
                bool phoneExists = _context.Customers.Any(c => c.Phone.ToLower() == trimmedPhone.ToLower());
                if (phoneExists)
                {
                    ModelState.AddModelError("Phone", $"A customer with phone number '{trimmedPhone}' already exists.");
                }
            }

            // 2. Email duplicate validation
            if (!string.IsNullOrEmpty(trimmedEmail))
            {
                bool emailExists = _context.Customers.Any(c => c.Email.ToLower() == trimmedEmail.ToLower());
                if (emailExists)
                {
                    ModelState.AddModelError("Email", $"A customer with email '{trimmedEmail}' already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                customer.Name = trimmedName;
                customer.Phone = trimmedPhone;
                customer.Email = trimmedEmail;

                _context.Customers.Add(customer);
                _context.SaveChanges();
                TempData["Success"] = "Customer added!";
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        // 4. EDIT (Show Form - Admin, Manager)
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // 5. EDIT (Update in Database - Admin, Manager)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(Customer customer)
        {
            ModelState.Remove("Sales");

            string trimmedName = customer.Name?.Trim() ?? string.Empty;
            string trimmedPhone = customer.Phone?.Trim() ?? string.Empty;
            string trimmedEmail = customer.Email?.Trim() ?? string.Empty;

            // 1. Phone number duplicate validation (excluding current customer)
            if (!string.IsNullOrEmpty(trimmedPhone))
            {
                bool phoneExists = _context.Customers.Any(c => c.CustomerId != customer.CustomerId && c.Phone.ToLower() == trimmedPhone.ToLower());
                if (phoneExists)
                {
                    ModelState.AddModelError("Phone", $"Another customer with phone number '{trimmedPhone}' already exists.");
                }
            }

            // 2. Email duplicate validation (excluding current customer)
            if (!string.IsNullOrEmpty(trimmedEmail))
            {
                bool emailExists = _context.Customers.Any(c => c.CustomerId != customer.CustomerId && c.Email.ToLower() == trimmedEmail.ToLower());
                if (emailExists)
                {
                    ModelState.AddModelError("Email", $"Another customer with email '{trimmedEmail}' already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                var existing = _context.Customers.Find(customer.CustomerId);
                if (existing == null) return NotFound();

                existing.Name = trimmedName;
                existing.Phone = trimmedPhone;
                existing.Email = trimmedEmail;

                _context.SaveChanges();
                TempData["Success"] = "Customer updated!";
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        // 6. DELETE (Show Confirmation - Admin, Manager)
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Include(c => c.Sales).FirstOrDefault(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // 7. DELETE (Smart Unlink: Reassigns past sales to Walk-in, removes customer)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            var customer = _context.Customers.Include(c => c.Sales).FirstOrDefault(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            int salesCount = customer.Sales?.Count ?? 0;

            // Smart Unlink: Reassign past transactions to Walk-in Customer so receipts don't break
            if (customer.Sales != null && customer.Sales.Count > 0)
            {
                foreach (var sale in customer.Sales)
                {
                    sale.CustomerId = null;
                }
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();

            if (salesCount > 0)
            {
                TempData["Success"] = $"Customer '{customer.Name}' deleted. Their {salesCount} past order(s) were safely preserved as Walk-in sales.";
            }
            else
            {
                TempData["Success"] = $"Customer '{customer.Name}' deleted.";
            }
            
            return RedirectToAction("Index");
        }
    }
}
