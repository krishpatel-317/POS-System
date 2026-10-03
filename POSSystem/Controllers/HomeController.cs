using Microsoft.AspNetCore.Authorization;
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

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ViewData["Title"] = "Dashboard";

            string currentUsername = User.Identity?.Name ?? "User";
            var currentPosUser = _context.POSUsers
                .FirstOrDefault(u => u.Username == currentUsername || u.Name == currentUsername);

            int currentUserId = currentPosUser?.UserId ?? 0;
            bool isPrivileged = User.IsInRole("Admin") || User.IsInRole("Manager");
            ViewBag.IsPrivileged = isPrivileged;

            // 1. Basic Counts
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalCustomers = _context.Customers.Count();

            var today = DateTime.Today;

            if (isPrivileged)
            {
                // ADMIN / MANAGER VIEW: Store-wide financial metrics
                var storeTodaySales = _context.Sales
                    .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1))
                    .ToList();
                
                var storeAllSales = _context.Sales.ToList();
                decimal storeAllRevenue = storeAllSales.Sum(s => s.TotalAmount);

                ViewBag.TodayRevenue = storeTodaySales.Sum(s => s.TotalAmount);
                ViewBag.TodaySalesCount = storeTodaySales.Count;
                ViewBag.TotalSales = storeAllSales.Count;
                ViewBag.AllTimeRevenue = storeAllRevenue;
                ViewBag.AverageOrderValue = storeAllSales.Count > 0 ? (storeAllRevenue / storeAllSales.Count) : 0;

                // Recent sales from all store counters
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
                // CASHIER VIEW: Strictly their own counter stats
                var myTodaySales = _context.Sales
                    .Where(s => s.UserId == currentUserId && s.SaleDate >= today && s.SaleDate < today.AddDays(1))
                    .ToList();

                var myAllSales = _context.Sales
                    .Where(s => s.UserId == currentUserId)
                    .ToList();
                
                decimal myAllRevenue = myAllSales.Sum(s => s.TotalAmount);

                ViewBag.TodayRevenue = myTodaySales.Sum(s => s.TotalAmount);
                ViewBag.TodaySalesCount = myTodaySales.Count;
                ViewBag.TotalSales = myAllSales.Count;
                ViewBag.AllTimeRevenue = myAllRevenue;
                ViewBag.AverageOrderValue = myAllSales.Count > 0 ? (myAllRevenue / myAllSales.Count) : 0;

                // Recent sales from THIS cashier only
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
