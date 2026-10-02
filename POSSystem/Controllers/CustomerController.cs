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

        // 1. READ (List all customers)
        public IActionResult Index()
        {
            var customers = _context.Customers.ToList();
            return View(customers);
        }

        // 2. CREATE (Show Form)
        public IActionResult Create()
        {
            return View();
        }

        // 3. CREATE (Save to Database)
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

        // 4. EDIT (Show Form)
        public IActionResult Edit(int id)
        {
            var customer = _context.Customers.Find(id);
            return View(customer);
        }

        // 5. EDIT (Update in Database)
        [HttpPost]
        public IActionResult Edit(Customer customer)
        {
            ModelState.Remove("Sales");

            if (ModelState.IsValid)
            {
                _context.Customers.Update(customer);
                _context.SaveChanges();
                TempData["Success"] = "Customer updated!";
                return RedirectToAction("Index");
            }
            return View(customer);
        }

        // 6. DELETE (Show Confirmation)
        public IActionResult Delete(int id)
        {
            var customer = _context.Customers.Find(id);
            return View(customer);
        }

        // 7. DELETE (Remove from Database)
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var customer = _context.Customers.Include(c => c.Sales).FirstOrDefault(c => c.CustomerId == id);

            if (customer != null && customer.Sales.Count > 0)
            {
                TempData["Error"] = "Cannot delete this customer because they have past sales.";
                return RedirectToAction("Index");
            }

            if (customer != null)
            {
                _context.Customers.Remove(customer);
                _context.SaveChanges();
                TempData["Success"] = "Customer deleted!";
            }
            
            return RedirectToAction("Index");
        }
    }
}

