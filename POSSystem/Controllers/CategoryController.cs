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

            string trimmedName = category.Name?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());
                if (nameExists)
                {
                    ModelState.AddModelError("Name", $"A category named '{trimmedName}' already exists (names are case-insensitive).");
                }
            }

            if (ModelState.IsValid)
            {
                category.Name = trimmedName;
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

            string trimmedName = category.Name?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = await _context.Categories.AnyAsync(c => c.CategoryId != category.CategoryId && c.Name.ToLower() == trimmedName.ToLower());
                if (nameExists)
                {
                    ModelState.AddModelError("Name", $"Another category named '{trimmedName}' already exists (names are case-insensitive).");
                }
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Categories.FindAsync(category.CategoryId);
                if (existing == null) return NotFound();

                existing.Name = trimmedName;
                existing.GSTRate = category.GSTRate;
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

        // 7. DELETE (Smart Reassignment: Reassigns products to 'General' fallback, removes category)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (category == null) return NotFound();

            // Guard the default root category
            if (category.Name.Equals("General", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The default 'General' department cannot be deleted.";
                return RedirectToAction("Index");
            }

            int productCount = category.Products?.Count ?? 0;

            // Smart Reassignment: Find or create the default 'General' department
            var generalCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "General");
            if (generalCategory == null)
            {
                generalCategory = new Category { Name = "General", GSTRate = 18m };
                _context.Categories.Add(generalCategory);
                await _context.SaveChangesAsync();
            }

            // Move any products inside this category to General
            if (category.Products != null && category.Products.Count > 0)
            {
                foreach (var prod in category.Products)
                {
                    prod.CategoryId = generalCategory.CategoryId;
                }
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            
            if (productCount > 0)
            {
                TempData["Success"] = $"Category '{category.Name}' deleted! Its {productCount} product(s) were safely moved to 'General'.";
            }
            else
            {
                TempData["Success"] = $"Category '{category.Name}' deleted!";
            }

            return RedirectToAction("Index");
        }
    }
}
