using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            // ===== 1) TOP 15 BÁN CHẠY (dựa theo OrderItems thuộc đơn Done) =====
            // Group theo ProductName (vì bạn đang seed OrderItems.ProductName)
            var topNames = await _db.OrderItems
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status == "Done")
                .GroupBy(oi => oi.ProductName)
                .Select(g => new { Name = g.Key, Qty = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Qty)
                .Take(15)
                .ToListAsync();

            var nameList = topNames.Select(x => x.Name).ToList();

            // Lấy Product theo Name để hiển thị card (kèm Category/Brand)
            var bestSellers = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive && nameList.Contains(p.Name))
                .ToListAsync();

            // sắp xếp theo đúng thứ tự bán chạy
            bestSellers = bestSellers
                .OrderBy(p => nameList.IndexOf(p.Name))
                .ToList();

            // ===== 2) TIN NỔI BẬT: 6 bài mới nhất đã đăng =====
            var featuredNews = await _db.NewsPosts
                .Where(n => n.IsPublished)
                .OrderByDescending(n => n.PublishedAt)
                .Take(6)
                .ToListAsync();

            // ===== 3) THƯƠNG HIỆU ĐANG BÁN (SLIDE LẶP LẠI) =====
            var dbBrands = await _db.Brands
                .Include(b => b.Products)
                .OrderBy(b => b.Name)
                .ToListAsync();

            var brandVMs = new List<HomeBrandItemVM>();
            if (dbBrands.Any())
            {
                foreach (var b in dbBrands)
                {
                    var count = b.Products.Count(p => p.IsActive);
                    var nameLower = b.Name.ToLower();
                    string origin = "Chính hãng";
                    string tagline = "Thương hiệu quốc tế";
                    string accentColor = "#0284c7";
                    string badge = "CHÍNH HÃNG";
                    string logoType = "general";

                    if (nameLower.Contains("yonex"))
                    {
                        origin = "Tokyo, Japan";
                        tagline = "Số 1 Cầu Lông Thế Giới";
                        accentColor = "#0052cc";
                        badge = "TOP 1";
                        logoType = "yonex";
                    }
                    else if (nameLower.Contains("victor"))
                    {
                        origin = "Taipei, Taiwan";
                        tagline = "Tốc Độ & Uy Lực Đột Phá";
                        accentColor = "#0d47a1";
                        badge = "TỐC ĐỘ";
                        logoType = "victor";
                    }
                    else if (nameLower.Contains("lining") || nameLower.Contains("li-ning"))
                    {
                        origin = "Beijing, China";
                        tagline = "Khí Động Học & Mạnh Mẽ";
                        accentColor = "#dc2626";
                        badge = "NỔI BẬT";
                        logoType = "lining";
                    }
                    else if (nameLower.Contains("mizuno"))
                    {
                        origin = "Osaka, Japan";
                        tagline = "Cơ Khí Chính Xác & Bền Bỉ";
                        accentColor = "#1e3a8a";
                        badge = "CAO CẤP";
                        logoType = "mizuno";
                    }
                    else if (nameLower.Contains("kawasaki"))
                    {
                        origin = "Tokyo, Japan";
                        tagline = "Trợ Lực & Tối Ưu Chi Phí";
                        accentColor = "#059669";
                        badge = "QUỐC DÂN";
                        logoType = "kawasaki";
                    }

                    brandVMs.Add(new HomeBrandItemVM
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Origin = origin,
                        Tagline = tagline,
                        ProductCount = count,
                        AccentColor = accentColor,
                        BadgeText = badge,
                        LogoType = logoType
                    });
                }
            }
            else
            {
                // Fallback nếu DB trống
                brandVMs = new List<HomeBrandItemVM>
                {
                    new() { Id = 1, Name = "Yonex", Origin = "Tokyo, Japan", Tagline = "Số 1 Cầu Lông Thế Giới", ProductCount = 18, AccentColor = "#0052cc", BadgeText = "TOP 1", LogoType = "yonex" },
                    new() { Id = 2, Name = "Victor", Origin = "Taipei, Taiwan", Tagline = "Tốc Độ & Uy Lực Đột Phá", ProductCount = 14, AccentColor = "#0d47a1", BadgeText = "TỐC ĐỘ", LogoType = "victor" },
                    new() { Id = 3, Name = "Li-Ning", Origin = "Beijing, China", Tagline = "Khí Động Học & Mạnh Mẽ", ProductCount = 16, AccentColor = "#dc2626", BadgeText = "NỔI BẬT", LogoType = "lining" },
                    new() { Id = 4, Name = "Mizuno", Origin = "Osaka, Japan", Tagline = "Cơ Khí Chính Xác & Bền Bỉ", ProductCount = 8, AccentColor = "#1e3a8a", BadgeText = "CAO CẤP", LogoType = "mizuno" },
                    new() { Id = 5, Name = "Kawasaki", Origin = "Tokyo, Japan", Tagline = "Trợ Lực & Tối Ưu Chi Phí", ProductCount = 10, AccentColor = "#059669", BadgeText = "QUỐC DÂN", LogoType = "kawasaki" }
                };
            }

            var vm = new HomeVM
            {
                BestSellers = bestSellers,
                FeaturedNews = featuredNews,
                Brands = brandVMs
            };

            return View(vm);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
