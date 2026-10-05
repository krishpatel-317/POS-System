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

        // 1. READ (List all customers - all authenticated staff)
        public IActionResult Index()
        {
            var customers = _context.Customers.ToList();
            return View(customers);
        }

        // 2. CREATE (Show Form - all authenticated staff)
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
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // 5. EDIT (Update in Database - Admin, Manager)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
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
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            if (customer == null) return NotFound();

            return View(customer);
        }

        // 7. DELETE (Remove from Database - Admin, Manager)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult DeleteConfirmed(int id)
        {
            var customer = _context.Customers.Include(c => c.Sales).FirstOrDefault(c => c.CustomerId == id);
            if (customer == null) return NotFound();

            if (customer.Sales != null && customer.Sales.Count > 0)
            {
                TempData["Error"] = "Cannot delete this customer because they have past sales.";
                return RedirectToAction("Index");
            }

            _context.Customers.Remove(customer);
            _context.SaveChanges();
            TempData["Success"] = "Customer deleted!";
            
            return RedirectToAction("Index");
        }
    }
}
