using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;
using System.Diagnostics;

namespace POSSystem.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";

            string currentUserId = _userManager.GetUserId(User) ?? string.Empty;
            bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Manager");
            ViewBag.IsPrivileged = isPrivileged;

            // 1. Basic Counts
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalCustomers = _context.Customers.Count();

            var today = DateTime.Today;

            if (isPrivileged)
            {
                // ADMIN / MANAGER VIEW: Store-wide financial metrics computed directly in SQL Server (excluding voided)
                var todaySalesQuery = _context.Sales.Where(s => s.Status != "Voided" && s.SaleDate >= today && s.SaleDate < today.AddDays(1));
                int todayCount = todaySalesQuery.Count();
                decimal todayRev = todayCount > 0 ? todaySalesQuery.Sum(s => s.TotalAmount) : 0m;

                var allActiveSales = _context.Sales.Where(s => s.Status != "Voided");
                int allCount = allActiveSales.Count();
                decimal allRev = allCount > 0 ? allActiveSales.Sum(s => s.TotalAmount) : 0m;

                ViewBag.TodayRevenue = todayRev;
                ViewBag.TodaySalesCount = todayCount;
                ViewBag.TotalSales = allCount;
                ViewBag.AllTimeRevenue = allRev;
                ViewBag.AverageOrderValue = allCount > 0 ? (allRev / allCount) : 0m;

                // Recent sales from all store counters (top 5 only)
                ViewBag.RecentSales = _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.User)
                    .Include(s => s.Payment)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToList();
            }
            else
            {
                // CASHIER VIEW: Strictly their own counter stats computed directly in SQL Server (excluding voided)
                var myTodayQuery = _context.Sales.Where(s => s.Status != "Voided" && s.UserId == currentUserId && s.SaleDate >= today && s.SaleDate < today.AddDays(1));
                int myTodayCount = myTodayQuery.Count();
                decimal myTodayRev = myTodayCount > 0 ? myTodayQuery.Sum(s => s.TotalAmount) : 0m;

                var myAllQuery = _context.Sales.Where(s => s.Status != "Voided" && s.UserId == currentUserId);
                int myAllCount = myAllQuery.Count();
                decimal myAllRev = myAllCount > 0 ? myAllQuery.Sum(s => s.TotalAmount) : 0m;

                ViewBag.TodayRevenue = myTodayRev;
                ViewBag.TodaySalesCount = myTodayCount;
                ViewBag.TotalSales = myAllCount;
                ViewBag.AllTimeRevenue = myAllRev;
                ViewBag.AverageOrderValue = myAllCount > 0 ? (myAllRev / myAllCount) : 0m;

                // Recent sales from THIS cashier only (top 5 only)
                ViewBag.RecentSales = _context.Sales
                    .Where(s => s.UserId == currentUserId)
                    .Include(s => s.Customer)
                    .Include(s => s.User)
                    .Include(s => s.Payment)
                    .OrderByDescending(s => s.SaleDate)
                    .Take(5)
                    .ToList();
            }

            // Low Stock Alert Products (Stock <= 5)
            ViewBag.LowStockProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity <= 5)
                .OrderBy(p => p.StockQuantity)
                .Take(5)
                .ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
