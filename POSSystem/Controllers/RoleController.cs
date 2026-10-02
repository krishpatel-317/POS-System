using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class RoleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RoleController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ: List all roles
        public IActionResult Index()
        {
            var roles = _context.POSRoles.ToList();
            return View(roles);
        }

        // 2. CREATE (GET): Populate dropdown from UserRole Enum
        public IActionResult Create()
        {
            // Populate dropdown choices directly from UserRole enum
            ViewBag.EnumRoles = new SelectList(Enum.GetNames<UserRole>());
            return View();
        }

        // 3. CREATE (POST): Save role selected from Enum
        [HttpPost]
        public IActionResult Create(Role role)
        {
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                // Check if role name already exists in database
                if (_context.POSRoles.Any(r => r.Name == role.Name))
                {
                    TempData["Error"] = $"Role \"{role.Name}\" already exists!";
                    ViewBag.EnumRoles = new SelectList(Enum.GetNames<UserRole>(), role.Name);
                    return View(role);
                }

                _context.POSRoles.Add(role);
                _context.SaveChanges();
                TempData["Success"] = $"Role \"{role.Name}\" added successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.EnumRoles = new SelectList(Enum.GetNames<UserRole>(), role.Name);
            return View(role);
        }

        // 4. EDIT (GET)
        public IActionResult Edit(int id)
        {
            var role = _context.POSRoles.Find(id);
            if (role == null) return NotFound();

            ViewBag.EnumRoles = new SelectList(Enum.GetNames<UserRole>(), role.Name);
            return View(role);
        }

        // 5. EDIT (POST)
        [HttpPost]
        public IActionResult Edit(Role role)
        {
            ModelState.Remove("Users");

            if (ModelState.IsValid)
            {
                _context.POSRoles.Update(role);
                _context.SaveChanges();
                TempData["Success"] = "Role updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.EnumRoles = new SelectList(Enum.GetNames<UserRole>(), role.Name);
            return View(role);
        }

        // 6. DELETE (GET)
        public IActionResult Delete(int id)
        {
            var role = _context.POSRoles.Find(id);
            if (role == null) return NotFound();
            return View(role);
        }

        // 7. DELETE (POST)
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var role = _context.POSRoles.Include(r => r.Users).FirstOrDefault(r => r.RoleId == id);

            if (role != null && role.Users.Count > 0)
            {
                TempData["Error"] = $"Cannot delete \"{role.Name}\" because {role.Users.Count} user(s) are assigned to it.";
                return RedirectToAction("Index");
            }

            if (role != null)
            {
                _context.POSRoles.Remove(role);
                _context.SaveChanges();
                TempData["Success"] = "Role deleted successfully!";
            }
            
            return RedirectToAction("Index");
        }
    }
}

