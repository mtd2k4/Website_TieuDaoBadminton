using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CatalogController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("Danh-Muc")]
        [Route("Catalog")]
        public async Task<IActionResult> Index(
            string? cat, 
            string? sub, 
            int? brandId, 
            string? price, 
            int? minPrice, 
            int? maxPrice, 
            string? sort = "newest", 
            string? q = null)
        {
            // 1. Lấy toàn bộ sản phẩm đang hoạt động để tính toán count cây danh mục & lọc
            var allProducts = await _db.Products
                .Include(x => x.Brand)
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .ToListAsync();

            var brands = await _db.Brands.OrderBy(b => b.Name).ToListAsync();

            // 2. Định nghĩa Cấu trúc Cây Menu Cấp 1 & Cấp 2
            var categoriesTree = new List<CatalogCategoryItem>
            {
                new CatalogCategoryItem
                {
                    Slug = "vot",
                    Name = "Vợt cầu lông",
                    Icon = "🏸",
                    SubCategories = new List<CatalogSubCategoryItem>
                    {
                        new CatalogSubCategoryItem { Slug = "tan-cong", Name = "Vợt Tấn Công (Smash)" },
                        new CatalogSubCategoryItem { Slug = "toan-dien", Name = "Vợt Công Thủ Toàn Diện" },
                        new CatalogSubCategoryItem { Slug = "phan-tat", Name = "Vợt Tốc Độ & Phản Tạt" },
                        new CatalogSubCategoryItem { Slug = "tro-luc", Name = "Vợt Trợ Lực / Dễ Chơi" }
                    }
                },
                new CatalogCategoryItem
                {
                    Slug = "giay",
                    Name = "Giày cầu lông",
                    Icon = "👟",
                    SubCategories = new List<CatalogSubCategoryItem>
                    {
                        new CatalogSubCategoryItem { Slug = "dem-em", Name = "Giày Đệm Êm (Cushion)" },
                        new CatalogSubCategoryItem { Slug = "toc-do", Name = "Giày Tốc Độ & Nhẹ (Speed)" },
                        new CatalogSubCategoryItem { Slug = "wide", Name = "Giày Chân Bè (Wide Fit)" }
                    }
                },
                new CatalogCategoryItem
                {
                    Slug = "quan-ao",
                    Name = "Trang phục thể thao",
                    Icon = "👕",
                    SubCategories = new List<CatalogSubCategoryItem>
                    {
                        new CatalogSubCategoryItem { Slug = "ao-nam", Name = "Áo Thi Đấu & Training Nam" },
                        new CatalogSubCategoryItem { Slug = "ao-nu", Name = "Áo & Váy Cầu Lông Nữ" },
                        new CatalogSubCategoryItem { Slug = "quan-short", Name = "Quần Short & Quần Dài" }
                    }
                }
            };

            // 3. Tính số lượng sản phẩm cho từng Cấp 1 & Cấp 2
            foreach (var category in categoriesTree)
            {
                var catProducts = allProducts.Where(p => p.Category.Slug == category.Slug).ToList();
                category.Count = catProducts.Count;
                category.IsActive = string.Equals(cat, category.Slug, StringComparison.OrdinalIgnoreCase);

                foreach (var subCat in category.SubCategories)
                {
                    subCat.Count = catProducts.Count(p => MatchSubCategory(category.Slug, subCat.Slug, p));
                    subCat.IsActive = category.IsActive && string.Equals(sub, subCat.Slug, StringComparison.OrdinalIgnoreCase);
                }
            }

            // 4. Tính số lượng cho từng Brand
            var brandItems = brands.Select(b => new CatalogBrandItem
            {
                Id = b.Id,
                Name = b.Name,
                Count = allProducts.Count(p => p.BrandId == b.Id),
                IsActive = brandId.HasValue && brandId.Value == b.Id
            }).ToList();

            // 5. Áp dụng các bộ lọc lên danh sách hiển thị
            var query = allProducts.AsEnumerable();

            // Lọc theo Cấp 1
            if (!string.IsNullOrWhiteSpace(cat))
            {
                query = query.Where(p => string.Equals(p.Category.Slug, cat, StringComparison.OrdinalIgnoreCase));
            }

            // Lọc theo Cấp 2
            if (!string.IsNullOrWhiteSpace(sub))
            {
                var targetCat = cat ?? categoriesTree.FirstOrDefault(c => c.SubCategories.Any(s => s.Slug == sub))?.Slug ?? "";
                query = query.Where(p => MatchSubCategory(targetCat, sub, p));
            }

            // Lọc theo Hãng
            if (brandId.HasValue && brandId.Value > 0)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            // Lọc theo Mức giá (Slider minPrice/maxPrice & Presets)
            if (!string.IsNullOrWhiteSpace(price) && !minPrice.HasValue && !maxPrice.HasValue)
            {
                switch (price)
                {
                    case "under-1m":
                        minPrice = 0;
                        maxPrice = 1000000;
                        break;
                    case "1m-2m":
                        minPrice = 1000000;
                        maxPrice = 2000000;
                        break;
                    case "2m-4m":
                        minPrice = 2000000;
                        maxPrice = 4000000;
                        break;
                    case "over-4m":
                        minPrice = 4000000;
                        maxPrice = 5000000;
                        break;
                }
            }

            if (minPrice.HasValue && minPrice.Value > 0)
            {
                query = query.Where(p => (p.SalePrice ?? p.Price) >= minPrice.Value);
            }

            if (maxPrice.HasValue && maxPrice.Value > 0 && maxPrice.Value < 5000000)
            {
                query = query.Where(p => (p.SalePrice ?? p.Price) <= maxPrice.Value);
            }

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) || (p.Brand?.Name.ToLower().Contains(term) == true));
            }

            // Sắp xếp
            query = sort switch
            {
                "price-asc" => query.OrderBy(p => p.SalePrice ?? p.Price),
                "price-desc" => query.OrderByDescending(p => p.SalePrice ?? p.Price),
                "sale" => query.OrderByDescending(p => p.SalePrice.HasValue ? (p.Price - p.SalePrice.Value) : 0),
                "name-asc" => query.OrderBy(p => p.Name),
                _ => query.OrderByDescending(p => p.Id) // newest
            };

            var finalProducts = query.ToList();

            // 6. Tiêu đề hiển thị & Breadcrumbs
            string currentTitle = "Tất cả danh mục sản phẩm";
            string? currentParentName = null;
            string? currentSubName = null;

            if (!string.IsNullOrWhiteSpace(cat))
            {
                var foundCat = categoriesTree.FirstOrDefault(c => string.Equals(c.Slug, cat, StringComparison.OrdinalIgnoreCase));
                if (foundCat != null)
                {
                    currentParentName = foundCat.Name;
                    currentTitle = foundCat.Name;

                    if (!string.IsNullOrWhiteSpace(sub))
                    {
                        var foundSub = foundCat.SubCategories.FirstOrDefault(s => string.Equals(s.Slug, sub, StringComparison.OrdinalIgnoreCase));
                        if (foundSub != null)
                        {
                            currentSubName = foundSub.Name;
                            currentTitle = $"{foundCat.Name} - {foundSub.Name}";
                        }
                    }
                }
            }

            var vm = new CatalogViewModel
            {
                Products = finalProducts,
                Categories = categoriesTree,
                Brands = brandItems,
                ActiveCat = cat,
                ActiveSub = sub,
                ActiveBrandId = brandId,
                ActivePrice = price,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                ActiveSort = sort,
                SearchKeyword = q,
                CurrentTitle = currentTitle,
                CurrentParentName = currentParentName,
                CurrentSubName = currentSubName,
                TotalCount = finalProducts.Count
            };

            return View(vm);
        }

        // Logic phân loại cấp 2 thông minh dựa trên tên & tính năng sản phẩm
        private static bool MatchSubCategory(string catSlug, string subSlug, Product p)
        {
            var name = p.Name.ToLower();

            if (catSlug == "vot")
            {
                return subSlug switch
                {
                    "tan-cong" => name.Contains("astrox") || name.Contains("thruster") || name.Contains("tectonic") || name.Contains("axforce") || name.Contains("fortius") || (p.Price >= 3500000 && !name.Contains("nanoflare")),
                    "toan-dien" => name.Contains("arcsaber") || name.Contains("drivex") || name.Contains("calibar") || name.Contains("altius") || name.Contains("brave sword"),
                    "phan-tat" => name.Contains("nanoflare") || name.Contains("auraspeed") || name.Contains("jetspeed") || name.Contains("1000z") || name.Contains("800") || name.Contains("90k"),
                    "tro-luc" => name.Contains("kawasaki") || name.Contains("honor") || name.Contains("king") || name.Contains("77") || p.Price <= 2500000,
                    _ => true
                };
            }
            else if (catSlug == "giay")
            {
                return subSlug switch
                {
                    "dem-em" => name.Contains("shb") || name.Contains("comfort") || name.Contains("eclipsion") || name.Contains("p9200") || name.Contains("fang") || name.Contains("65z3"),
                    "toc-do" => name.Contains("aerus") || name.Contains("s82") || name.Contains("thunder") || name.Contains("aytt 005") || name.Contains("aytt 007"),
                    "wide" => name.Contains("a970") || name.Contains("a830") || name.Contains("claw") || name.Contains("k-068") || name.Contains("k-086") || name.Contains("k-099") || name.Contains("aytt 009") || name.Contains("aytt 011") || name.Contains("a311"),
                    _ => true
                };
            }
            else if (catSlug == "quan-ao")
            {
                return subSlug switch
                {
                    "ao-nam" => (name.Contains("áo") || name.Contains("polo") || name.Contains("thun")) && !name.Contains("nữ") && !name.Contains("váy"),
                    "ao-nu" => name.Contains("nữ") || name.Contains("váy") || name.Contains("cặp"),
                    "quan-short" => name.Contains("quần") || name.Contains("short") || name.Contains("khoác") || name.Contains("bộ") || name.Contains("set"),
                    _ => true
                };
            }

            return true;
        }
    }
}
