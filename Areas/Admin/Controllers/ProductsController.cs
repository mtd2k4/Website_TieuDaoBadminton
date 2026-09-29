using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Admin/Products
        public async Task<IActionResult> Index(string? search, int? categoryId, string? status, string? stockFilter, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var q = _db.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.Variants)
                .AsQueryable();

            // Tính thống kê kho hàng toàn hệ thống trước khi lọc
            var allProducts = await _db.Products.Include(x => x.Variants).ToListAsync();
            ViewBag.TotalProducts = allProducts.Count;
            ViewBag.TotalInventoryUnits = allProducts.Sum(p => p.Variants.Sum(v => v.Stock));
            ViewBag.LowStockCount = allProducts.Count(p => p.Variants.Sum(v => v.Stock) > 0 && p.Variants.Sum(v => v.Stock) <= 5);
            ViewBag.OutOfStockCount = allProducts.Count(p => p.Variants.Sum(v => v.Stock) == 0);

            // 1. Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(x => x.Name.Contains(s) || x.Variants.Any(v => v.Sku != null && v.Sku.Contains(s)));
            }

            // 2. Lọc theo danh mục
            if (categoryId.HasValue)
                q = q.Where(x => x.CategoryId == categoryId.Value);

            // 3. Lọc theo trạng thái kinh doanh
            if (status == "active") q = q.Where(x => x.IsActive);
            if (status == "inactive") q = q.Where(x => !x.IsActive);

            var filteredList = await q.OrderByDescending(x => x.Id).ToListAsync();

            // 4. Lọc theo tình trạng kho hàng
            if (stockFilter == "instock")
            {
                filteredList = filteredList.Where(p => p.Variants.Sum(v => v.Stock) > 5).ToList();
            }
            else if (stockFilter == "lowstock")
            {
                filteredList = filteredList.Where(p => p.Variants.Sum(v => v.Stock) > 0 && p.Variants.Sum(v => v.Stock) <= 5).ToList();
            }
            else if (stockFilter == "outofstock")
            {
                filteredList = filteredList.Where(p => p.Variants.Sum(v => v.Stock) == 0).ToList();
            }

            // 5. Tính toán phân trang
            var totalCount = filteredList.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var pagedList = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.StartItem = totalCount == 0 ? 0 : (page - 1) * pageSize + 1;
            ViewBag.EndItem = Math.Min(page * pageSize, totalCount);

            ViewBag.Categories = await _db.Categories
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString(), categoryId == x.Id))
                .ToListAsync();

            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "";
            ViewBag.StockFilter = stockFilter ?? "";

            return View(pagedList);
        }

        // GET: /Admin/Products/Create
        public async Task<IActionResult> Create()
        {
            return View(await BuildVM());
        }

        // POST: /Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminProductFormVM vm)
        {
            if (!ModelState.IsValid)
                return View(await BuildVM(vm));

            var p = new Product
            {
                Name = vm.Name.Trim(),
                CategoryId = vm.CategoryId,
                BrandId = vm.BrandId,
                Price = vm.Price,
                SalePrice = vm.SalePrice,
                Description = vm.Description,
                IsActive = vm.IsActive
            };

            // Upload ảnh nếu có
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                p.MainImageUrl = await SaveFile(vm.ImageFile, "uploads/products");

            // Nếu không upload thì dùng URL người dùng nhập
            if (string.IsNullOrWhiteSpace(p.MainImageUrl))
                p.MainImageUrl = vm.MainImageUrl;

            _db.Products.Add(p);
            await _db.SaveChangesAsync();

            // Khởi tạo số lượng kho ban đầu
            var sizes = string.IsNullOrWhiteSpace(vm.InitialSizes)
                ? Array.Empty<string>()
                : vm.InitialSizes.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var colors = string.IsNullOrWhiteSpace(vm.InitialColors)
                ? Array.Empty<string>()
                : vm.InitialColors.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            var initialStock = Math.Max(0, vm.InitialStock);
            var baseSku = string.IsNullOrWhiteSpace(vm.Sku) ? $"SP-{p.Id:D4}" : vm.Sku.Trim();

            var variantsCreated = new List<ProductVariant>();

            if (sizes.Length > 0 && colors.Length > 0)
            {
                foreach (var size in sizes)
                {
                    foreach (var color in colors)
                    {
                        var v = new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = size,
                            Color = color,
                            Stock = initialStock,
                            Sku = $"{baseSku}-{size}-{color}"
                        };
                        variantsCreated.Add(v);
                        _db.ProductVariants.Add(v);
                    }
                }
            }
            else if (sizes.Length > 0)
            {
                foreach (var size in sizes)
                {
                    var v = new ProductVariant
                    {
                        ProductId = p.Id,
                        Size = size,
                        Color = null,
                        Stock = initialStock,
                        Sku = $"{baseSku}-{size}"
                    };
                    variantsCreated.Add(v);
                    _db.ProductVariants.Add(v);
                }
            }
            else if (colors.Length > 0)
            {
                foreach (var color in colors)
                {
                    var v = new ProductVariant
                    {
                        ProductId = p.Id,
                        Size = null,
                        Color = color,
                        Stock = initialStock,
                        Sku = $"{baseSku}-{color}"
                    };
                    variantsCreated.Add(v);
                    _db.ProductVariants.Add(v);
                }
            }
            else
            {
                var v = new ProductVariant
                {
                    ProductId = p.Id,
                    Size = null,
                    Color = null,
                    Stock = initialStock,
                    Sku = baseSku
                };
                variantsCreated.Add(v);
                _db.ProductVariants.Add(v);
            }

            await _db.SaveChangesAsync();

            // Ghi nhật ký nhập kho ban đầu
            foreach (var v in variantsCreated)
            {
                _db.StockReceipts.Add(new StockReceipt
                {
                    ProductId = p.Id,
                    ProductVariantId = v.Id,
                    QuantityAdded = initialStock,
                    PreviousStock = 0,
                    NewStock = initialStock,
                    Note = "Khởi tạo tồn kho ban đầu khi tạo sản phẩm",
                    CreatedBy = User?.Identity?.Name ?? "Admin",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã thêm sản phẩm '{p.Name}' với kho hàng ban đầu ({initialStock} chiếc/phân loại).";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Products
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            var vm = await BuildVM(new AdminProductFormVM
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = p.CategoryId,
                BrandId = p.BrandId,
                Price = p.Price,
                SalePrice = p.SalePrice,
                MainImageUrl = p.MainImageUrl,
                Description = p.Description,
                IsActive = p.IsActive,
                InitialStock = p.Variants.Sum(v => v.Stock),
                ExistingVariants = p.Variants.Select(v => new ProductVariantItemVM
                {
                    Id = v.Id,
                    Size = v.Size,
                    Color = v.Color,
                    Stock = v.Stock,
                    Sku = v.Sku
                }).ToList()
            });

            // Lấy 5 lần nhập kho gần nhất của sản phẩm
            ViewBag.RecentReceipts = await _db.StockReceipts
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Take(6)
                .ToListAsync();

            return View(vm);
        }

        // POST: /Admin/Products/Edit/5
        // Chú ý: TỒN KHO KHÔNG CHO PHÉP SỬA TRỰC TIẾP Ở ĐÂY (chỉ cho phép cập nhật thông tin sản phẩm và mã SKU)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminProductFormVM vm, List<int>? variantIds, List<string>? variantSkus)
        {
            var p = await _db.Products
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == vm.Id);

            if (p == null) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.ExistingVariants = p.Variants.Select(v => new ProductVariantItemVM
                {
                    Id = v.Id,
                    Size = v.Size,
                    Color = v.Color,
                    Stock = v.Stock,
                    Sku = v.Sku
                }).ToList();
                return View(await BuildVM(vm));
            }

            p.Name = vm.Name.Trim();
            p.CategoryId = vm.CategoryId;
            p.BrandId = vm.BrandId;
            p.Price = vm.Price;
            p.SalePrice = vm.SalePrice;
            p.Description = vm.Description;
            p.IsActive = vm.IsActive;

            // Upload ảnh mới nếu có
            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
                p.MainImageUrl = await SaveFile(vm.ImageFile, "uploads/products");
            else if (!string.IsNullOrWhiteSpace(vm.MainImageUrl))
                p.MainImageUrl = vm.MainImageUrl;

            // Cập nhật mã SKU của các biến thể (tồn kho KHÔNG bị ghi đè, tuân thủ nguyên tắc quản lý kho)
            if (variantIds != null && variantSkus != null)
            {
                for (int i = 0; i < variantIds.Count; i++)
                {
                    var vid = variantIds[i];
                    var variant = p.Variants.FirstOrDefault(v => v.Id == vid);
                    if (variant != null && i < variantSkus.Count)
                    {
                        variant.Sku = variantSkus[i]?.Trim();
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = $"Đã cập nhật thông tin sản phẩm: {p.Name}. (Số lượng tồn kho được bảo toàn).";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Products/Restock
        // CHỨC NĂNG NHẬP HÀNG: TĂNG DẦN SỐ LƯỢNG TỒN KHO & GHI NHẬT KÝ KHO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restock(int productId, int? variantId, int quantity, string? note, string? returnUrl)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng nhập hàng phải lớn hơn 0.";
                if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
                return RedirectToAction(nameof(Index));
            }

            var p = await _db.Products
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (p == null) return NotFound();

            ProductVariant? targetVariant = null;

            if (variantId.HasValue)
            {
                targetVariant = p.Variants.FirstOrDefault(v => v.Id == variantId.Value);
            }
            else if (p.Variants.Count == 1)
            {
                targetVariant = p.Variants.First();
            }

            if (targetVariant != null)
            {
                // Nhập cho 1 phân loại cụ thể
                int prevStock = targetVariant.Stock;
                targetVariant.Stock += quantity; // TĂNG DẦN TỒN KHO

                _db.StockReceipts.Add(new StockReceipt
                {
                    ProductId = productId,
                    ProductVariantId = targetVariant.Id,
                    QuantityAdded = quantity,
                    PreviousStock = prevStock,
                    NewStock = targetVariant.Stock,
                    Note = string.IsNullOrWhiteSpace(note) ? "Nhập thêm hàng vào kho" : note.Trim(),
                    CreatedBy = User?.Identity?.Name ?? "Admin",
                    CreatedAt = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                var variantLabel = !string.IsNullOrEmpty(targetVariant.Size) ? $" (Size: {targetVariant.Size})" : "";
                TempData["Ok"] = $"✓ Nhập hàng thành công! Đã tăng thêm +{quantity} chiếc cho '{p.Name}{variantLabel}'. Tồn kho mới: {targetVariant.Stock} chiếc.";
            }
            else if (p.Variants.Count > 1)
            {
                // Nếu sản phẩm có nhiều phân loại và không chọn phân loại cụ thể: chia đều hoặc tăng cho từng phân loại
                foreach (var v in p.Variants)
                {
                    int prevStock = v.Stock;
                    v.Stock += quantity;

                    _db.StockReceipts.Add(new StockReceipt
                    {
                        ProductId = productId,
                        ProductVariantId = v.Id,
                        QuantityAdded = quantity,
                        PreviousStock = prevStock,
                        NewStock = v.Stock,
                        Note = string.IsNullOrWhiteSpace(note) ? "Nhập hàng đồng loạt cho tất cả phân loại" : note.Trim(),
                        CreatedBy = User?.Identity?.Name ?? "Admin",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
                TempData["Ok"] = $"✓ Nhập hàng thành công! Đã tăng thêm +{quantity} chiếc cho mỗi phân loại của '{p.Name}'.";
            }
            else
            {
                // Sản phẩm chưa có biến thể nào: tự tạo biến thể chuẩn
                var v = new ProductVariant
                {
                    ProductId = productId,
                    Size = null,
                    Color = null,
                    Stock = quantity,
                    Sku = $"SP-{productId:D4}"
                };
                _db.ProductVariants.Add(v);
                await _db.SaveChangesAsync();

                _db.StockReceipts.Add(new StockReceipt
                {
                    ProductId = productId,
                    ProductVariantId = v.Id,
                    QuantityAdded = quantity,
                    PreviousStock = 0,
                    NewStock = quantity,
                    Note = "Tạo phân loại kho và nhập hàng lần đầu",
                    CreatedBy = User?.Identity?.Name ?? "Admin",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();

                TempData["Ok"] = $"✓ Nhập hàng thành công! Đã nhập +{quantity} chiếc vào kho.";
            }

            if (!string.IsNullOrEmpty(returnUrl)) return LocalRedirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Products/AddVariant
        // Thêm phân loại mới kèm số lượng nhập ban đầu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddVariant(int productId, string? size, string? color, int stock, string? sku)
        {
            var p = await _db.Products.FindAsync(productId);
            if (p == null) return NotFound();

            var initialStock = Math.Max(0, stock);
            var variant = new ProductVariant
            {
                ProductId = productId,
                Size = string.IsNullOrWhiteSpace(size) ? null : size.Trim(),
                Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim(),
                Stock = initialStock,
                Sku = string.IsNullOrWhiteSpace(sku) ? $"SP-{productId:D4}-{(size ?? "DEF")}" : sku.Trim()
            };

            _db.ProductVariants.Add(variant);
            await _db.SaveChangesAsync();

            if (initialStock > 0)
            {
                _db.StockReceipts.Add(new StockReceipt
                {
                    ProductId = productId,
                    ProductVariantId = variant.Id,
                    QuantityAdded = initialStock,
                    PreviousStock = 0,
                    NewStock = initialStock,
                    Note = $"Khai báo phân loại mới ({size ?? "-"} / {color ?? "-"}) kèm nhập kho ban đầu",
                    CreatedBy = User?.Identity?.Name ?? "Admin",
                    CreatedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            TempData["Ok"] = $"Đã thêm phân loại mới vào kho: {size ?? "Mặc định"} - {color ?? ""} (SL ban đầu: {initialStock})";
            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        // POST: /Admin/Products/DeleteVariant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int variantId, int productId)
        {
            var variant = await _db.ProductVariants.FindAsync(variantId);
            if (variant != null)
            {
                _db.ProductVariants.Remove(variant);
                await _db.SaveChangesAsync();
                TempData["Ok"] = "Đã xóa phân loại kho.";
            }

            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        // POST: /Admin/Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products
                .Include(x => x.Variants)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            _db.Products.Remove(p);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã xóa sản phẩm: {p.Name}";
            return RedirectToAction(nameof(Index));
        }

        // ===== helpers =====
        private async Task<AdminProductFormVM> BuildVM(AdminProductFormVM? vm = null)
        {
            vm ??= new AdminProductFormVM();

            vm.Categories = await _db.Categories.OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString(), vm.CategoryId == x.Id))
                .ToListAsync();

            vm.Brands = await _db.Brands.OrderBy(x => x.Name)
                .Select(x => new SelectListItem(x.Name, x.Id.ToString(), vm.BrandId == x.Id))
                .ToListAsync();

            return vm;
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) ext = ".jpg";

            var name = $"{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, folder.Replace("/", Path.DirectorySeparatorChar.ToString()));
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, name);
            using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);

            return "/" + folder.Trim('/') + "/" + name;
        }
    }
}
