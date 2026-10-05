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
            var query = _context.Products.Where(p => !p.IsArchived).Include(p => p.Category).AsQueryable();

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
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Categories = GetCategorySelectList();
            return View();
        }

        // 3. CREATE (Save to Database)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            string trimmedName = product.Name?.Trim() ?? string.Empty;
            string trimmedSku = product.SKU?.Trim() ?? string.Empty;

            // 1. Case-insensitive duplicate product name validation among active products
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = await _context.Products.AnyAsync(p => !p.IsArchived && p.Name.ToLower() == trimmedName.ToLower());
                if (nameExists)
                {
                    ModelState.AddModelError("Name", $"A product named '{trimmedName}' already exists (names are case-insensitive).");
                }
            }

            // 2. Barcode / SKU uniqueness validation among active products
            if (!string.IsNullOrEmpty(trimmedSku))
            {
                bool skuExists = await _context.Products.AnyAsync(p => !p.IsArchived && p.SKU.ToLower() == trimmedSku.ToLower());
                if (skuExists)
                {
                    ModelState.AddModelError("SKU", $"Barcode / SKU '{trimmedSku}' is already assigned to another product.");
                }
            }

            if (ModelState.IsValid)
            {
                product.Name = trimmedName;
                product.SKU = trimmedSku;
                product.IsArchived = false;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{product.Name}' added to inventory!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = GetCategorySelectList(product.CategoryId);
            return View(product);
        }

        // 4. EDIT (PRIVILEGE: Only Admin and Manager can adjust prices or stock)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null || product.IsArchived) return NotFound();

            ViewBag.Categories = GetCategorySelectList(product.CategoryId);
            return View(product);
        }

        // 5. EDIT (Update in Database)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            string trimmedName = product.Name?.Trim() ?? string.Empty;
            string trimmedSku = product.SKU?.Trim() ?? string.Empty;

            // 1. Case-insensitive duplicate product name validation (excluding current product)
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = await _context.Products.AnyAsync(p => !p.IsArchived && p.ProductId != product.ProductId && p.Name.ToLower() == trimmedName.ToLower());
                if (nameExists)
                {
                    ModelState.AddModelError("Name", $"Another product named '{trimmedName}' already exists (names are case-insensitive).");
                }
            }

            // 2. Barcode / SKU uniqueness validation (excluding current product)
            if (!string.IsNullOrEmpty(trimmedSku))
            {
                bool skuExists = await _context.Products.AnyAsync(p => !p.IsArchived && p.ProductId != product.ProductId && p.SKU.ToLower() == trimmedSku.ToLower());
                if (skuExists)
                {
                    ModelState.AddModelError("SKU", $"Barcode / SKU '{trimmedSku}' is already assigned to another product.");
                }
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Products.FindAsync(product.ProductId);
                if (existing == null || existing.IsArchived) return NotFound();

                existing.Name = trimmedName;
                existing.SKU = trimmedSku;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                existing.CategoryId = product.CategoryId;

                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{existing.Name}' updated!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = GetCategorySelectList(product.CategoryId);
            return View(product);
        }

        // Helper to populate category dropdown with GST rate indicator
        private SelectList GetCategorySelectList(int? selectedId = null)
        {
            var categories = _context.Categories
                .OrderBy(c => c.Name)
                .Select(c => new
                {
                    c.CategoryId,
                    DisplayName = $"{c.Name} ({c.GSTRate:0.##}% GST)"
                })
                .ToList();

            return new SelectList(categories, "CategoryId", "DisplayName", selectedId);
        }

        // 6. DELETE (PRIVILEGE: Only Admin and Manager can delete products)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.Include(p => p.Category).Include(p => p.SaleItems).FirstOrDefaultAsync(p => p.ProductId == id && !p.IsArchived);
            if (product == null) return NotFound();

            return View(product);
        }

        // 7. DELETE (Smart Deletion: safely archives sold items to keep receipts, deletes unsold items completely)
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Include(p => p.SaleItems).FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();

            // If product was sold in past transactions: Soft archive so past bills never crash
            if (product.SaleItems != null && product.SaleItems.Count > 0)
            {
                product.IsArchived = true;
                product.StockQuantity = 0;
                product.SKU = string.Empty; // Free up SKU/barcode so it can be reused
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{product.Name}' removed from catalog. Past sales receipts were preserved.";
                return RedirectToAction("Index");
            }

            // If product was never sold: Hard delete completely
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Product '{product.Name}' deleted.";
            return RedirectToAction("Index");
        }

        // 8. RESTOCK (Quick Reorder / Stock In - Admin & Manager)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restock(int id, int additionalStock, string? returnUrl)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (additionalStock <= 0)
            {
                TempData["Error"] = "Restock quantity must be at least 1 unit.";
            }
            else
            {
                product.StockQuantity += additionalStock;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Successfully restocked +{additionalStock} units for '{product.Name}'. Current stock: {product.StockQuantity}.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }
    }
}
