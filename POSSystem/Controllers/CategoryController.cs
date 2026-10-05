using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data;
using POSSystem.Models;

namespace POSSystem.Controllers
{
    [Authorize]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. READ (List all categories - all authenticated staff)
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.Include(c => c.Products).ToListAsync();
            return View(categories);
        }

        // 2. CREATE (Show Form - Admin, Manager)
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View();
        }

        // 3. CREATE (Save to Database - Admin, Manager)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(Category category)
        {
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

        // 4. EDIT (Show Form - Admin, Manager)
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 5. EDIT (Update in Database - Admin, Manager)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Category category)
        {
            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                var existing = await _context.Categories.FindAsync(category.CategoryId);
                if (existing == null) return NotFound();

                existing.Name = category.Name;
                await _context.SaveChangesAsync();
                
                TempData["Success"] = "Category updated!";
                return RedirectToAction("Index");
            }
            
            return View(category);
        }

        // 6. DELETE (Show Confirmation - Admin, Manager)
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound();

            return View(category);
        }

        // 7. DELETE (Remove from Database - Admin, Manager)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound();

            // Simple check: don't delete if it has products
            if (category.Products != null && category.Products.Count > 0)
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
