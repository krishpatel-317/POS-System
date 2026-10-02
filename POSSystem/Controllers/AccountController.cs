using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    // AccountController handles Login, Register, and Logout using ASP.NET Core Identity
    // This uses the built-in Microsoft Identity system (no external providers)
    public class AccountController : Controller
    {
        // UserManager: creates and manages Identity users (AspNetUsers table)
        private readonly UserManager<IdentityUser> _userManager;

        // SignInManager: handles login / logout cookie
        private readonly SignInManager<IdentityUser> _signInManager;

        public AccountController(UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ── GET: /Account/Login ──────────────────────────────────────────────
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            // If already logged in, go to dashboard
            if (User.Identity!.IsAuthenticated)
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

            // Try to sign in using Identity cookie authentication
            var result = await _signInManager.PasswordSignInAsync(
                model.Username,
                model.Password,
                isPersistent: false,   // Don't keep logged in after browser closes
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                TempData["Success"] = "Welcome back! You are now logged in.";
                return RedirectToAction("Index", "Home");
            }

            // Login failed
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

            // Create a new Identity user
            var user = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            // Identity creates user with hashed password automatically
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Automatically log in after registration
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["Success"] = "Account created! Welcome to the POS System.";
                return RedirectToAction("Index", "Home");
            }

            // Show any Identity errors (e.g. "password too short")
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
    }
}
