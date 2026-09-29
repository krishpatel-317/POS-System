using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;
using System.Diagnostics;

namespace POSSystem.Controllers
{
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

            // 1. Basic Counts
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalCustomers = _context.Customers.Count();
            ViewBag.TotalSales = _context.Sales.Count();

            // 2. Today's Revenue (Simple sum)
            var today = DateTime.Today;
            var todaySales = _context.Sales.Where(s => s.SaleDate.Date == today).ToList();
            
            decimal totalRevenue = 0;
            foreach (var sale in todaySales)
            {
                totalRevenue += sale.TotalAmount;
            }
            ViewBag.TodayRevenue = totalRevenue;

            // 3. Low Stock Products (Simple LINQ)
            ViewBag.LowStockProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity <= 5)
                .Take(5)
                .ToList();

            // 4. Recent Sales (Simple OrderBy)
            ViewBag.RecentSales = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.User)
                .OrderByDescending(s => s.SaleDate)
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
