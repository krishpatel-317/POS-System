using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize(Roles = "Admin")]
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
                string trimmedUsername = model.Username?.Trim() ?? string.Empty;
                string trimmedEmail = model.Email?.Trim() ?? string.Empty;

                // Check if username already exists in Identity
                var existingIdentity = await _userManager.FindByNameAsync(trimmedUsername);
                if (existingIdentity != null)
                {
                    ModelState.AddModelError("Username", "A staff user with this username already exists.");
                    ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                    return View(model);
                }

                // Check if email already exists in Identity
                if (!string.IsNullOrWhiteSpace(trimmedEmail))
                {
                    var existingEmail = await _userManager.FindByEmailAsync(trimmedEmail);
                    if (existingEmail != null)
                    {
                        ModelState.AddModelError("Email", "A staff user with this email address already exists.");
                        ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                        return View(model);
                    }
                }

                var identityUser = new IdentityUser
                {
                    UserName = trimmedUsername,
                    Email = string.IsNullOrWhiteSpace(trimmedEmail) ? $"{trimmedUsername}@posstore.local" : trimmedEmail
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

            if (currentRole == "Admin" && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "Managers are not permitted to edit Administrator accounts.";
                return RedirectToAction("Index");
            }

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

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains("Admin") && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "Managers are not permitted to edit Administrator accounts.";
                return RedirectToAction("Index");
            }

            if (model.Role == "Admin" && !User.IsInRole("Admin"))
            {
                ModelState.AddModelError("Role", "Only Administrators can assign the Admin role.");
            }

            if (ModelState.IsValid)
            {
                string trimmedUsername = model.Username?.Trim() ?? string.Empty;
                string trimmedEmail = model.Email?.Trim() ?? string.Empty;

                // Check if username is taken by another user
                var existingUser = await _userManager.FindByNameAsync(trimmedUsername);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Username", "Another staff user with this username already exists.");
                    ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                    return View(model);
                }

                // Check if email is taken by another user
                if (!string.IsNullOrWhiteSpace(trimmedEmail))
                {
                    var existingEmail = await _userManager.FindByEmailAsync(trimmedEmail);
                    if (existingEmail != null && existingEmail.Id != user.Id)
                    {
                        ModelState.AddModelError("Email", "Another staff user with this email address already exists.");
                        ViewBag.Roles = new SelectList(Enum.GetNames<UserRole>(), model.Role);
                        return View(model);
                    }
                }

                user.UserName = trimmedUsername;
                user.Email = trimmedEmail;
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

        // 6. DELETE (Confirm removal - Admin only)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["Error"] = "Administrator accounts cannot be deleted.";
                return RedirectToAction("Index");
            }

            return View(new StaffUserViewModel
            {
                Id = user.Id,
                Username = user.UserName ?? "",
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "Cashier"
            });
        }

        // 7. DELETE (Smart Unlink - Admin only)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                TempData["Error"] = "Administrator accounts cannot be deleted.";
                return RedirectToAction("Index");
            }

            // Prevent deleting the currently logged-in account
            string currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            if (user.Id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own logged-in account.";
                return RedirectToAction("Index");
            }

            var pastSales = await _context.Sales.Where(s => s.UserId == id).ToListAsync();
            int salesCount = pastSales.Count;

            // Smart Unlink: Reassign their past sales to former staff (UserId = null) so receipts don't break
            if (salesCount > 0)
            {
                foreach (var sale in pastSales)
                {
                    sale.UserId = null;
                }
                await _context.SaveChangesAsync();
            }

            await _userManager.DeleteAsync(user);

            if (salesCount > 0)
            {
                TempData["Success"] = $"Staff member '{user.UserName}' deleted. Their {salesCount} processed sale(s) were safely retained in store records.";
            }
            else
            {
                TempData["Success"] = $"Staff member '{user.UserName}' deleted.";
            }

            return RedirectToAction("Index");
        }
    }
}
