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
            ViewBag.Categories = GetCategorySelectList();
            return View();
        }

        // 3. CREATE (Save to Database)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            string trimmedName = product.Name?.Trim() ?? string.Empty;
            string trimmedSku = product.SKU?.Trim() ?? string.Empty;

            // 1. Case-insensitive duplicate product name validation
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = await _context.Products.AnyAsync(p => p.Name.ToLower() == trimmedName.ToLower());
                if (nameExists)
                {
                    ModelState.AddModelError("Name", $"A product named '{trimmedName}' already exists (names are case-insensitive).");
                }
            }

            // 2. Barcode / SKU uniqueness validation (if SKU is provided)
            if (!string.IsNullOrEmpty(trimmedSku))
            {
                bool skuExists = await _context.Products.AnyAsync(p => p.SKU.ToLower() == trimmedSku.ToLower());
                if (skuExists)
                {
                    ModelState.AddModelError("SKU", $"Barcode / SKU '{trimmedSku}' is already assigned to another product.");
                }
            }

            if (ModelState.IsValid)
            {
                product.Name = trimmedName;
                product.SKU = trimmedSku;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Product '{product.Name}' added to inventory!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = GetCategorySelectList(product.CategoryId);
            return View(product);
        }

        // 4. EDIT (PRIVILEGE: Only Admin and Manager can adjust prices or stock)
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = GetCategorySelectList(product.CategoryId);
            return View(product);
        }

        // 5. EDIT (Update in Database)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(Product product)
        {
            ModelState.Remove("Category");
            ModelState.Remove("SaleItems");

            string trimmedName = product.Name?.Trim() ?? string.Empty;
            string trimmedSku = product.SKU?.Trim() ?? string.Empty;

            // 1. Case-insensitive duplicate product name validation (excluding current product)
            if (!string.IsNullOrEmpty(trimmedName))
            {
                bool nameExists = await _context.Products.AnyAsync(p => p.ProductId != product.ProductId && p.Name.ToLower() == trimmedName.ToLower());
                if (nameExists)
                {
                    ModelState.AddModelError("Name", $"Another product named '{trimmedName}' already exists (names are case-insensitive).");
                }
            }

            // 2. Barcode / SKU uniqueness validation (excluding current product)
            if (!string.IsNullOrEmpty(trimmedSku))
            {
                bool skuExists = await _context.Products.AnyAsync(p => p.ProductId != product.ProductId && p.SKU.ToLower() == trimmedSku.ToLower());
                if (skuExists)
                {
                    ModelState.AddModelError("SKU", $"Barcode / SKU '{trimmedSku}' is already assigned to another product.");
                }
            }

            if (ModelState.IsValid)
            {
                var existing = await _context.Products.FindAsync(product.ProductId);
                if (existing == null) return NotFound();

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
            if (product == null) return NotFound();

            // Delete Guard: Cannot delete product if it was sold in historical transactions
            if (product.SaleItems != null && product.SaleItems.Count > 0)
            {
                TempData["Error"] = $"Cannot delete product '{product.Name}' because it exists in past sales receipts. Archive or reduce stock to 0 instead.";
                return RedirectToAction("Index");
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = $"Product '{product.Name}' deleted from inventory.";
            return RedirectToAction("Index");
        }

        // 8. RESTOCK (Quick Reorder / Stock In - Admin & Manager)
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
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
