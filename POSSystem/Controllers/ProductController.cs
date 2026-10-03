using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ (All staff can view products to check stock, price, and category)
        public async Task<IActionResult> Index(string search, int categoryId)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            var products = await query.ToListAsync();
            return View(products);
        }

        // 2. CREATE (PRIVILEGE: Only Admin and Manager can add new inventory items)
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // 3. CREATE (Save to Database)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{product.Name}' added to inventory!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 4. EDIT (PRIVILEGE: Only Admin and Manager can adjust prices or stock)
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 5. EDIT (Update in Database)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{product.Name}' updated!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 6. DELETE (PRIVILEGE: Only Admin and Manager can delete products)
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            return View(product);
        }

        // 7. DELETE (Remove from Database with SaleItem guard)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Include(p => p.SaleItems).FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                // Delete Guard: Cannot delete product if it was sold in historical transactions
                if (product.SaleItems != null && product.SaleItems.Count > 0)
                {
                    TempData["Error"] = $"Cannot delete product '{product.Name}' because it exists in past sales receipts. Archive or reduce stock to 0 instead.";
                    return RedirectToAction("Index");
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{product.Name}' deleted from inventory.";
            }

            return RedirectToAction("Index");
        }
    }
}
