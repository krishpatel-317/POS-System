using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ (List all products, with simple search & filter)
        public async Task<IActionResult> Index(string search, int categoryId)
        {
            // Start with a basic query that includes the Category data
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // Apply search if the user typed something
            if (search != null)
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            // Apply category filter if a category was selected
            if (categoryId > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            // Send data back to the view so the dropdowns/textboxes keep their values
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = await _context.Categories.ToListAsync();

            var products = await query.ToListAsync();
            return View(products);
        }

        // 2. CREATE (Show Form)
        public IActionResult Create()
        {
            // Send the list of categories to the view to create a dropdown list
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name");
            return View();
        }

        // 3. CREATE (Save to Database)
        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            // Ignore these fields during validation since they aren't filled in by the form
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            if (ModelState.IsValid)
            {
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Product added!";
                return RedirectToAction("Index");
            }

            // If there's an error, recreate the dropdown list and show the form again
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 4. EDIT (Show Form)
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            
            // Recreate the dropdown, selecting the product's current category
            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 5. EDIT (Update in Database)
        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Product updated!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(_context.Categories, "CategoryId", "Name", product.CategoryId);
            return View(product);
        }

        // 6. DELETE (Show Confirmation)
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.ProductId == id);
            return View(product);
        }

        // 7. DELETE (Remove from Database)
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Include(p => p.SaleItems).FirstOrDefaultAsync(p => p.ProductId == id);

            // Simple check: don't delete if it's already part of a sale
            if (product.SaleItems.Count > 0)
            {
                TempData["Error"] = "Cannot delete this product because it is in a past sale.";
                return RedirectToAction("Index");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Product deleted!";
            return RedirectToAction("Index");
        }
    }
}
