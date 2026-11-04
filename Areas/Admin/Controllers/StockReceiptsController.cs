using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Areas.Admin.ViewModels;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class StockReceiptsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public StockReceiptsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/StockReceipts
        // Danh sách phiếu nhập hàng có phân trang
        public async Task<IActionResult> Index(string? search, string? supplier, int page = 1)
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var q = _db.StockReceipts
                .Include(r => r.Product)
                    .ThenInclude(p => p.Brand)
                .Include(r => r.ProductVariant)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(r => r.Product.Name.Contains(s)
                              || (r.BatchNumber != null && r.BatchNumber.Contains(s))
                              || r.SupplierName.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(supplier))
            {
                q = q.Where(r => r.SupplierName == supplier);
            }

            // Thống kê toàn diện các phiếu nhập
            var allReceipts = await _db.StockReceipts.ToListAsync();
            ViewBag.TotalReceiptsCount = allReceipts.Count;
            ViewBag.TotalUnitsImported = allReceipts.Sum(r => r.QuantityAdded);
            ViewBag.TotalImportAmount = allReceipts.Sum(r => r.TotalImportCost);
            ViewBag.TotalSuppliersCount = allReceipts.Select(r => r.SupplierName).Distinct().Count();

            // Danh sách nhà cung cấp để lọc
            ViewBag.Suppliers = allReceipts
                .Where(r => !string.IsNullOrEmpty(r.SupplierName))
                .Select(r => r.SupplierName)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var totalCount = await q.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var list = await q.OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;
            ViewBag.StartItem = totalCount == 0 ? 0 : (page - 1) * pageSize + 1;
            ViewBag.EndItem = Math.Min(page * pageSize, totalCount);

            ViewBag.Search = search ?? "";
            ViewBag.Supplier = supplier ?? "";

            return View(list);
        }

        // GET: /Admin/StockReceipts/Create
        // Trang tạo phiếu nhập hàng riêng biệt
        public async Task<IActionResult> Create(int? productId)
        {
            var vm = new StockReceiptFormVM();

            var products = await _db.Products
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .OrderBy(p => p.Name)
                .ToListAsync();

            vm.ProductsList = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} ({p.Brand?.Name}) - Giá bán: {p.Price:n0} đ",
                Selected = productId.HasValue && productId.Value == p.Id
            }).ToList();

            if (productId.HasValue)
            {
                vm.ProductId = productId.Value;
                var selProduct = products.FirstOrDefault(p => p.Id == productId.Value);
                if (selProduct != null)
                {
                    // Gợi ý giá vốn nhập khoảng 60% giá bán
                    vm.UnitImportPrice = Math.Round((selProduct.Price * 0.62m) / 1000m) * 1000m;
                    vm.SupplierName = $"{selProduct.Brand?.Name} Official Distributor";

                    vm.VariantsList = selProduct.Variants.Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"[Màu: {(!string.IsNullOrWhiteSpace(v.Color) && v.Color != "-" ? v.Color : "Tiêu chuẩn")}] [Size: {(!string.IsNullOrWhiteSpace(v.Size) && v.Size != "-" ? v.Size : "Tiêu chuẩn")}] (Tồn hiện tại: {v.Stock})"
                    }).ToList();
                }
            }

            return View(vm);
        }

        // POST: /Admin/StockReceipts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockReceiptFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(vm);
                return View(vm);
            }

            var p = await _db.Products
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == vm.ProductId);

            if (p == null)
            {
                ModelState.AddModelError("ProductId", "Không tìm thấy sản phẩm đã chọn.");
                await PopulateDropdowns(vm);
                return View(vm);
            }

            ProductVariant? targetVariant = null;

            if (vm.ProductVariantId.HasValue)
            {
                targetVariant = p.Variants.FirstOrDefault(v => v.Id == vm.ProductVariantId.Value);
            }
            else if (p.Variants.Count == 1)
            {
                targetVariant = p.Variants.First();
            }

            int prevStock = 0;
            int newStock = 0;
            decimal totalCost = vm.UnitImportPrice * vm.Quantity;

            if (targetVariant != null)
            {
                prevStock = targetVariant.Stock;
                targetVariant.Stock += vm.Quantity; // TĂNG DẦN SỐ LƯỢNG KHO
                newStock = targetVariant.Stock;
            }
            else if (p.Variants.Count > 1)
            {
                // Nếu sản phẩm có nhiều phân loại và không chọn cụ thể, tăng cho phân loại đầu tiên
                targetVariant = p.Variants.First();
                prevStock = targetVariant.Stock;
                targetVariant.Stock += vm.Quantity;
                newStock = targetVariant.Stock;
            }
            else
            {
                // Chưa có biến thể nào -> tạo biến thể tiêu chuẩn
                targetVariant = new ProductVariant
                {
                    ProductId = p.Id,
                    Size = null,
                    Color = null,
                    Stock = vm.Quantity,
                    Sku = $"SP-{p.Id:D4}"
                };
                _db.ProductVariants.Add(targetVariant);
                await _db.SaveChangesAsync();
                prevStock = 0;
                newStock = vm.Quantity;
            }

            var receipt = new StockReceipt
            {
                ProductId = p.Id,
                ProductVariantId = targetVariant?.Id,
                QuantityAdded = vm.Quantity,
                PreviousStock = prevStock,
                NewStock = newStock,
                SupplierName = vm.SupplierName.Trim(),
                OriginCountry = string.IsNullOrWhiteSpace(vm.OriginCountry) ? "Chính hãng" : vm.OriginCountry.Trim(),
                BatchNumber = string.IsNullOrWhiteSpace(vm.BatchNumber) ? $"LÔ-{DateTime.Now:yyyyMMdd}-{p.Id}" : vm.BatchNumber.Trim(),
                UnitImportPrice = vm.UnitImportPrice,
                TotalImportCost = totalCost,
                Note = vm.Note?.Trim(),
                CreatedBy = User?.Identity?.Name ?? "Thủ kho",
                CreatedAt = DateTime.UtcNow
            };

            _db.StockReceipts.Add(receipt);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Nhập kho thành công phiếu #{receipt.Id:D5}! Đã nhập +{vm.Quantity} chiếc từ {receipt.SupplierName}. Tổng chi phí nhập: {totalCost:n0} đ. Tồn kho mới: {newStock} chiếc.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/StockReceipts/Report
        // BẢNG THỐNG KÊ ĐỐI CHIẾU BÁN VÀ NHẬP - TÍNH PHẦN TRĂM LÃI SUẤT TỪNG SẢN PHẨM (CÓ PHÂN TRANG)
        public async Task<IActionResult> Report(
            string? search = null,
            int? categoryId = null,
            string? marginFilter = null,
            string? sortBy = "revenue_desc",
            int page = 1,
            int pageSize = 10)
        {
            var summary = new ProfitComparisonSummaryVM();

            // 1. Lấy danh sách sản phẩm cùng các phiếu nhập và các mặt hàng đã bán
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .ToListAsync();

            var receipts = await _db.StockReceipts.ToListAsync();

            // Đơn hàng đã hoàn thành (Done)
            var doneOrderItems = await _db.OrderItems
                .Include(i => i.Order)
                .Where(i => i.Order.Status == "Done")
                .ToListAsync();

            var allReportItems = new List<ProductProfitReportItemVM>();

            foreach (var p in products)
            {
                var prodReceipts = receipts.Where(r => r.ProductId == p.Id).ToList();
                var prodOrderItems = doneOrderItems.Where(i => i.ProductName == p.Name || i.ProductName.Contains(p.Name)).ToList();

                int totalImported = prodReceipts.Sum(r => r.QuantityAdded);
                int totalSold = prodOrderItems.Sum(i => i.Quantity);
                int currentStock = p.Variants.Sum(v => v.Stock);

                decimal retailPrice = p.SalePrice ?? p.Price;

                // Đơn giá nhập: Lấy đơn giá nhập gần nhất hoặc trung bình từ phiếu nhập; nếu chưa có phiếu, mặc định định mức ~62% giá niêm yết
                decimal unitCost = 0;
                if (prodReceipts.Any(r => r.UnitImportPrice > 0))
                {
                    unitCost = prodReceipts.OrderByDescending(r => r.CreatedAt).First(r => r.UnitImportPrice > 0).UnitImportPrice;
                }
                else
                {
                    unitCost = Math.Round((p.Price * 0.62m) / 1000m) * 1000m; // Mức giá vốn chuẩn ngành thể thao
                }

                decimal unitProfit = retailPrice - unitCost;
                double marginPercent = retailPrice > 0 ? (double)(unitProfit / retailPrice * 100) : 0;
                double markupPercent = unitCost > 0 ? (double)(unitProfit / unitCost * 100) : 0;

                decimal revenue = totalSold * retailPrice;
                decimal costSold = totalSold * unitCost;
                decimal grossProfit = revenue - costSold;

                allReportItems.Add(new ProductProfitReportItemVM
                {
                    ProductId = p.Id,
                    CategoryId = p.CategoryId,
                    ProductName = p.Name,
                    ImageUrl = p.MainImageUrl,
                    CategoryName = p.Category?.Name ?? "-",
                    BrandName = p.Brand?.Name ?? "-",
                    RetailPrice = retailPrice,
                    UnitCostPrice = unitCost,
                    UnitProfit = unitProfit,
                    ProfitMarginPercent = Math.Round(marginPercent, 1),
                    MarkupPercent = Math.Round(markupPercent, 1),
                    TotalImportedUnits = totalImported,
                    TotalSoldUnits = totalSold,
                    CurrentStock = currentStock,
                    TotalRevenue = revenue,
                    TotalCostSold = costSold,
                    GrossProfit = grossProfit
                });
            }

            // 2. Thống kê KPI tổng thể toàn cửa hàng (trước khi lọc/phân trang)
            summary.TotalRevenue = allReportItems.Sum(r => r.TotalRevenue);
            summary.TotalCostOfGoodsSold = allReportItems.Sum(r => r.TotalCostSold);
            summary.TotalImportExpenses = receipts.Sum(r => r.TotalImportCost);
            summary.TotalGrossProfit = summary.TotalRevenue - summary.TotalCostOfGoodsSold;

            summary.AverageMarginPercent = summary.TotalRevenue > 0
                ? Math.Round((double)(summary.TotalGrossProfit / summary.TotalRevenue * 100), 1)
                : 0;

            summary.AverageMarkupPercent = summary.TotalCostOfGoodsSold > 0
                ? Math.Round((double)(summary.TotalGrossProfit / summary.TotalCostOfGoodsSold * 100), 1)
                : 0;

            // 3. Lọc danh sách sản phẩm theo điều kiện tìm kiếm
            var filteredList = allReportItems.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim();
                filteredList = filteredList.Where(r =>
                    r.ProductName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.BrandName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.CategoryName.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                filteredList = filteredList.Where(r => r.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(marginFilter))
            {
                filteredList = marginFilter switch
                {
                    "super" => filteredList.Where(r => r.ProfitMarginPercent >= 45),
                    "good" => filteredList.Where(r => r.ProfitMarginPercent >= 30 && r.ProfitMarginPercent < 45),
                    "normal" => filteredList.Where(r => r.ProfitMarginPercent >= 20 && r.ProfitMarginPercent < 30),
                    "low" => filteredList.Where(r => r.ProfitMarginPercent < 20),
                    _ => filteredList
                };
            }

            // 4. Sắp xếp
            filteredList = sortBy switch
            {
                "margin_desc" => filteredList.OrderByDescending(r => r.ProfitMarginPercent).ThenByDescending(r => r.GrossProfit),
                "margin_asc" => filteredList.OrderBy(r => r.ProfitMarginPercent),
                "profit_desc" => filteredList.OrderByDescending(r => r.GrossProfit).ThenByDescending(r => r.TotalRevenue),
                "revenue_desc" => filteredList.OrderByDescending(r => r.TotalRevenue).ThenByDescending(r => r.GrossProfit),
                "sold_desc" => filteredList.OrderByDescending(r => r.TotalSoldUnits).ThenByDescending(r => r.TotalRevenue),
                "name_asc" => filteredList.OrderBy(r => r.ProductName),
                _ => filteredList.OrderByDescending(r => r.TotalRevenue).ThenByDescending(r => r.GrossProfit)
            };

            var finalSortedList = filteredList.ToList();

            // 5. Phân trang
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 10;

            summary.TotalItems = finalSortedList.Count;
            summary.Page = page;
            summary.PageSize = pageSize;
            summary.Search = search;
            summary.CategoryId = categoryId;
            summary.MarginFilter = marginFilter;
            summary.SortBy = sortBy;

            // Load Categories list for filter select
            var categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            summary.CategoriesList = categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name,
                Selected = categoryId.HasValue && categoryId.Value == c.Id
            }).ToList();

            summary.ReportItems = finalSortedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View(summary);
        }

        // GET: /Admin/StockReceipts/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var receipt = await _db.StockReceipts
                .Include(r => r.Product)
                    .ThenInclude(p => p.Brand)
                .Include(r => r.Product)
                    .ThenInclude(p => p.Category)
                .Include(r => r.ProductVariant)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (receipt == null) return NotFound();

            return View(receipt);
        }

        // API AJAX helper: Lấy các phân loại & gợi ý giá khi chọn sản phẩm
        [HttpGet]
        public async Task<IActionResult> GetProductVariants(int productId)
        {
            var p = await _db.Products
                .Include(x => x.Brand)
                .Include(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Id == productId);

            if (p == null) return NotFound();

            var suggestedCost = Math.Round((p.Price * 0.62m) / 1000m) * 1000m;
            var supplier = $"{p.Brand?.Name} Official Distributor";

            var variants = p.Variants.Select(v => new
            {
                id = v.Id,
                name = $"[Màu: {(!string.IsNullOrWhiteSpace(v.Color) && v.Color != "-" ? v.Color : "Tiêu chuẩn")}] [Size: {(!string.IsNullOrWhiteSpace(v.Size) && v.Size != "-" ? v.Size : "Tiêu chuẩn")}] (Tồn hiện tại: {v.Stock})"
            }).ToList();

            return Json(new
            {
                suggestedCost,
                supplier,
                retailPrice = p.Price,
                variants
            });
        }

        private async Task PopulateDropdowns(StockReceiptFormVM vm)
        {
            var products = await _db.Products.Include(p => p.Brand).OrderBy(p => p.Name).ToListAsync();
            vm.ProductsList = products.Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = $"{p.Name} ({p.Brand?.Name})",
                Selected = p.Id == vm.ProductId
            }).ToList();

            if (vm.ProductId > 0)
            {
                var selProduct = await _db.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == vm.ProductId);
                if (selProduct != null)
                {
                    vm.VariantsList = selProduct.Variants.Select(v => new SelectListItem
                    {
                        Value = v.Id.ToString(),
                        Text = $"Size: {v.Size ?? "Chuẩn"} {(string.IsNullOrWhiteSpace(v.Color) ? "" : $"- {v.Color}")} (Tồn: {v.Stock})"
                    }).ToList();
                }
            }
        }
    }
}
