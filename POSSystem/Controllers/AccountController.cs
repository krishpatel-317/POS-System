using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    // AccountController handles Login, Register, and Logout using ASP.NET Core Identity
    // Supports Role-based Authentication & Authorization
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 RoleManager<IdentityRole> roleManager,
                                 ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // ── GET: /Account/Login ──────────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // ── POST: /Account/Login ─────────────────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                var identityUser = await _userManager.FindByNameAsync(model.Username);
                var roles = identityUser != null ? await _userManager.GetRolesAsync(identityUser) : new List<string>();
                string roleText = roles.Any() ? string.Join(", ", roles) : "Staff";

                TempData["Success"] = $"Welcome back, {model.Username}! Logged in as {roleText}.";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        // ── GET: /Account/Register ───────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // ── POST: /Account/Register ──────────────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign selected role (Admin, Manager, or Cashier)
                string roleName = string.IsNullOrWhiteSpace(model.Role) ? "Cashier" : model.Role;
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
                await _userManager.AddToRoleAsync(user, roleName);

                // Ensure POS Role exists
                var posRole = _context.POSRoles.FirstOrDefault(r => r.Name == roleName)
                              ?? _context.POSRoles.FirstOrDefault();
                if (posRole == null)
                {
                    posRole = new Role { Name = roleName };
                    _context.POSRoles.Add(posRole);
                    _context.SaveChanges();
                }

                // Sync into POS Users table for cashier tracking
                string displayName = string.IsNullOrWhiteSpace(model.Name) ? model.Username : model.Name;
                var posUser = new User
                {
                    Name = displayName,
                    Username = model.Username,
                    Password = "IdentityManaged",
                    RoleId = posRole.RoleId
                };
                _context.POSUsers.Add(posUser);
                _context.SaveChanges();

                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = $"Account created for {displayName}! Role assigned: {roleName}.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // ── POST: /Account/Logout ────────────────────────────────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            TempData["Success"] = "You have been logged out successfully.";
            return RedirectToAction("Login", "Account");
        }

        // ── GET: /Account/AccessDenied ───────────────────────────────────────
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
