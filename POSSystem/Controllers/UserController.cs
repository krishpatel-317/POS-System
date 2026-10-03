using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    // PRIVILEGE: Admin and Manager can view staff; only Admin can create, edit, or delete staff accounts
    [Authorize(Roles = "Admin,Manager")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. READ (Admin & Manager can view staff list)
        public IActionResult Index()
        {
            var users = _context.POSUsers.Include(u => u.Role).ToList();
            return View(users);
        }

        // 2. CREATE (Show Add Staff Form - ADMIN ONLY)
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name");
            return View();
        }

        // 3. CREATE (Create both POSUser and ASP.NET Core Identity account - ADMIN ONLY)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(User user)
        {
            ModelState.Remove("Role");
            ModelState.Remove("Sales");

            if (ModelState.IsValid)
            {
                // Check if username already exists in Identity
                var existingIdentity = await _userManager.FindByNameAsync(user.Username);
                if (existingIdentity != null)
                {
                    ModelState.AddModelError("Username", "A staff user with this username already exists.");
                    ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
                    return View(user);
                }

                // 1. Create Identity login account
                var identityUser = new IdentityUser
                {
                    UserName = user.Username,
                    Email = $"{user.Username}@posstore.local"
                };

                var createResult = await _userManager.CreateAsync(identityUser, user.Password);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
                    return View(user);
                }

                // 2. Assign Identity Role (Admin, Manager, Cashier)
                var role = _context.POSRoles.Find(user.RoleId);
                if (role != null)
                {
                    await _userManager.AddToRoleAsync(identityUser, role.Name);
                }

                // 3. Save in POS Users table for cashier tracking
                _context.POSUsers.Add(user);
                _context.SaveChanges();

                TempData["Success"] = $"Staff member '{user.Name}' ({role?.Name ?? "Staff"}) created successfully! They can now log in.";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
            return View(user);
        }

        // 4. EDIT (Show edit staff form - ADMIN ONLY)
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var user = _context.POSUsers.Find(id);
            if (user == null) return NotFound();

            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
            return View(user);
        }

        // 5. EDIT (Update staff details and role - ADMIN ONLY)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(User user)
        {
            ModelState.Remove("Role");
            ModelState.Remove("Sales");

            if (ModelState.IsValid)
            {
                var existingPos = _context.POSUsers.Find(user.UserId);
                if (existingPos == null) return NotFound();

                existingPos.Name = user.Name;
                existingPos.RoleId = user.RoleId;

                // If password was changed, update in Identity too
                var identityUser = await _userManager.FindByNameAsync(existingPos.Username);
                if (identityUser != null)
                {
                    var newRole = _context.POSRoles.Find(user.RoleId);
                    if (newRole != null)
                    {
                        var currentRoles = await _userManager.GetRolesAsync(identityUser);
                        await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                        await _userManager.AddToRoleAsync(identityUser, newRole.Name);
                    }

                    if (!string.IsNullOrWhiteSpace(user.Password) && user.Password != existingPos.Password)
                    {
                        var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
                        await _userManager.ResetPasswordAsync(identityUser, token, user.Password);
                        existingPos.Password = user.Password;
                    }
                }

                _context.SaveChanges();
                TempData["Success"] = "Staff profile updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = new SelectList(_context.POSRoles, "RoleId", "Name", user.RoleId);
            return View(user);
        }

        // 6. DELETE (Confirm removal - ADMIN ONLY)
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var user = _context.POSUsers.Include(u => u.Role).FirstOrDefault(u => u.UserId == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // 7. DELETE (Delete staff account with protection - ADMIN ONLY)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = _context.POSUsers.Include(u => u.Sales).FirstOrDefault(u => u.UserId == id);

            if (user != null)
            {
                // Delete Guard: Cannot delete cashier who has recorded sales
                if (user.Sales != null && user.Sales.Any())
                {
                    TempData["Error"] = $"Cannot remove staff member '{user.Name}' because they have processed {user.Sales.Count} sales transaction(s). Sales history must remain intact.";
                    return RedirectToAction("Index");
                }

                // Remove from Identity
                var identityUser = await _userManager.FindByNameAsync(user.Username);
                if (identityUser != null)
                {
                    await _userManager.DeleteAsync(identityUser);
                }

                _context.POSUsers.Remove(user);
                _context.SaveChanges();

                TempData["Success"] = $"Staff member '{user.Name}' deleted.";
            }

            return RedirectToAction("Index");
        }
    }
}
