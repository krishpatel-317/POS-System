using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ (List all categories)
        public async Task<IActionResult> Index()
        {
            // Simple query to get all categories and include their products
            var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
            return View(categories);
        }

        // 2. CREATE (Show Form)
        public IActionResult Create()
        {
            return View();
        }

        // 3. CREATE (Save to Database)
        [HttpPost]
        public async Task<IActionResult> Create(Category category)
        {
            // Ignore the Products list during validation since the form only sends the Name
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Category added!";
                return RedirectToAction("Index");
            }
            
            return View(category);
        }

        // 4. EDIT (Show Form)
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            return View(category);
        }

        // 5. EDIT (Update in Database)
        [HttpPost]
        public async Task<IActionResult> Edit(Category category)
        {
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Category updated!";
                return RedirectToAction("Index");
            }
            
            return View(category);
        }

        // 6. DELETE (Show Confirmation)
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            return View(category);
        }

        // 7. DELETE (Remove from Database)
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);

            // Simple check: don't delete if it has products
            if (category.Products.Count > 0)
            {
                TempData["Error"] = "Cannot delete this category because it has products inside it.";
                return RedirectToAction("Index");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Category deleted!";
            return RedirectToAction("Index");
        }
    }
}
