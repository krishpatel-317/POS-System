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

            if (ModelState.IsValid)
            {
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

            if (ModelState.IsValid)
            {
                var existing = _context.Customers.Find(customer.CustomerId);
                if (existing == null) return NotFound();

                existing.Name = customer.Name;
                existing.Phone = customer.Phone;
                existing.Email = customer.Email;

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
