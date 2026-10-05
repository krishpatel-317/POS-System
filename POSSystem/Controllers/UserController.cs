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
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // 1. READ (List all staff users with their Identity role and sales count)
        public async Task<IActionResult> Index()
        {
            var identityUsers = await _userManager.Users.ToListAsync();
            var userList = new List<StaffUserViewModel>();

            foreach (var user in identityUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                int salesCount = await _context.Sales.CountAsync(s => s.UserId == user.Id);

                userList.Add(new StaffUserViewModel
                {
                    Id = user.Id,
                    Username = user.UserName ?? "User",
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "Cashier",
                    SalesCount = salesCount
                });
            }

            return View(userList);
        }

        // 2. CREATE (Show Add Staff Form)
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>());
            return View(new StaffUserViewModel());
        }

        // 3. CREATE (Create ASP.NET Core Identity account and assign role)
        [HttpPost]
        public async Task<IActionResult> Create(StaffUserViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("Password", "Password is required for new accounts.");
            }

            if (ModelState.IsValid)
            {
                // Check if username already exists in Identity
                var existingIdentity = await _userManager.FindByNameAsync(model.Username);
                if (existingIdentity != null)
                {
                    ModelState.AddModelError("Username", "A staff user with this username already exists.");
                    ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                    return View(model);
                }

                var identityUser = new IdentityUser
                {
                    UserName = model.Username,
                    Email = string.IsNullOrWhiteSpace(model.Email) ? $"{model.Username}@posstore.local" : model.Email
                };

                var createResult = await _userManager.CreateAsync(identityUser, model.Password!);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                    return View(model);
                }

                // Ensure role exists and assign
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }
                await _userManager.AddToRoleAsync(identityUser, model.Role);

                TempData["Success"] = $"Staff member '{identityUser.UserName}' ({model.Role}) created successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
            return View(model);
        }

        // 4. EDIT (Show edit staff form)
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var currentRole = roles.FirstOrDefault() ?? "Cashier";

            ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), currentRole);
            return View(new StaffUserViewModel
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = currentRole
            });
        }

        // 5. EDIT (Update staff details, role, and optional password)
        [HttpPost]
        public async Task<IActionResult> Edit(StaffUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            if (ModelState.IsValid)
            {
                user.UserName = model.Username;
                user.Email = model.Email;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                    return View(model);
                }

                // Update Role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }
                    await _userManager.AddToRoleAsync(user, model.Role);
                }

                // If password was changed, update it
                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!resetResult.Succeeded)
                    {
                        foreach (var error in resetResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                        return View(model);
                    }
                }

                TempData["Success"] = "Staff profile updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
            return View(model);
        }

        // 6. DELETE (Confirm removal)
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return View(new StaffUserViewModel
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "Cashier"
            });
        }

        // 7. DELETE (Delete staff account with sales protection)
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Delete Guard: Cannot delete staff who has processed sales
            bool hasSales = await _context.Sales.AnyAsync(s => s.UserId == id);
            if (hasSales)
            {
                int salesCount = await _context.Sales.CountAsync(s => s.UserId == id);
                TempData["Error"] = $"Cannot remove staff member '{user.UserName}' because they have processed {salesCount} sales transaction(s). Sales history must remain intact.";
                return RedirectToAction("Index");
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = $"Staff member '{user.UserName}' deleted.";
            return RedirectToAction("Index");
        }
    }
}
