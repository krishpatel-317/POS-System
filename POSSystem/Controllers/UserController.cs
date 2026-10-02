using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var users = _context.POSUsers.Include(u => u.Role).ToList();
            return View(users);
        }

        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name");
            return View();
        }

        [HttpPost]
        public IActionResult Create(User user)
        {
            ModelState.Remove("Role");
            ModelState.Remove("Sales");

            if (ModelState.IsValid)
            {
                _context.POSUsers.Add(user);
                _context.SaveChanges();
                TempData["Success"] = "User added!";
                return RedirectToAction("Index");
            }
            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
            return View(user);
        }

        public IActionResult Edit(int id)
        {
            var user = _context.POSUsers.Find(id);
            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user?.RoleId);
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user)
        {
            ModelState.Remove("Role");
            ModelState.Remove("Sales");

            if (ModelState.IsValid)
            {
                _context.POSUsers.Update(user);
                _context.SaveChanges();
                TempData["Success"] = "User updated!";
                return RedirectToAction("Index");
            }
            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
            return View(user);
        }

        public IActionResult Delete(int id)
        {
            var user = _context.POSUsers.Include(u => u.Role).FirstOrDefault(u => u.UserId == id);
            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var user = _context.POSUsers.Include(u => u.Sales).FirstOrDefault(u => u.UserId == id);

            if (user != null && user.Sales.Count > 0)
            {
                TempData["Error"] = "Cannot delete this user because they processed sales.";
                return RedirectToAction("Index");
            }

            if (user != null)
            {
                _context.POSUsers.Remove(user);
                _context.SaveChanges();
                TempData["Success"] = "User deleted!";
            }
            
            return RedirectToAction("Index");
        }
    }
}


