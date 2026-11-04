using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public ProductsController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index(string? category, string? q)
        {
            var queries = new List<string>();
            if (!string.IsNullOrWhiteSpace(category))
                queries.Add($"cat={category}");
            if (!string.IsNullOrWhiteSpace(q))
                queries.Add($"q={Uri.EscapeDataString(q)}");

            var targetUrl = queries.Count > 0 ? $"/Danh-Muc?{string.Join("&", queries)}" : "/Danh-Muc";
            return RedirectPermanent(targetUrl);
        }


        [HttpGet("Products/Detail/{id:int}")]
        [HttpGet("San-Pham/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var p = await _db.Products
                .Include(x => x.Brand)
                .Include(x => x.Category)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (p == null) return NotFound();

            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == id)
                .ToListAsync();

            int GetSizeSortWeight(string? size)
            {
                if (string.IsNullOrWhiteSpace(size)) return 999;
                var s = size.Trim().ToUpper();
                if (s == "XS") return 10;
                if (s == "S") return 20;
                if (s == "M") return 30;
                if (s == "L") return 40;
                if (s == "XL") return 50;
                if (s == "XXL" || s == "2XL") return 60;
                if (s == "XXXL" || s == "3XL") return 70;
                if (s.StartsWith("5U")) return 110;
                if (s.StartsWith("4U")) return 120;
                if (s.StartsWith("3U")) return 130;
                if (s.StartsWith("2U")) return 140;
                if (int.TryParse(s, out var num)) return 200 + num;
                return 500;
            }

            variants = variants
                .OrderBy(v => GetSizeSortWeight(v.Size))
                .ThenBy(v => v.Color)
                .ToList();

            ViewBag.Variants = variants;

            // 4 sản phẩm liên quan cùng danh mục
            ViewBag.RelatedProducts = await _db.Products
                .Include(x => x.Brand)
                .Include(x => x.Category)
                .Where(x => x.CategoryId == p.CategoryId && x.Id != id && x.IsActive)
                .Take(4)
                .ToListAsync();

            // Đánh giá & Xếp hạng (Reviews & Ratings)
            var reviews = await _db.ProductReviews
                .Where(r => r.ProductId == id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            double avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 5.0;
            int reviewCount = reviews.Count;
            var starCounts = new int[6];
            foreach (var r in reviews)
            {
                if (r.Rating >= 1 && r.Rating <= 5)
                    starCounts[r.Rating]++;
            }

            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = Math.Round(avgRating, 1);
            ViewBag.ReviewCount = reviewCount;
            ViewBag.StarCounts = starCounts;

            return View(p);
        }

        // POST: /Products/AddReview (Yêu cầu đăng nhập mới được phép đánh giá)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string? title, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Vui lòng nhập nội dung đánh giá của bạn.";
                return Redirect($"/Products/Detail/{productId}#reviews-section");
            }

            if (rating < 1 || rating > 5) rating = 5;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "Vui lòng đăng nhập tài khoản để gửi đánh giá.";
                return Redirect($"/Account/Login?returnUrl=/Products/Detail/{productId}#reviews-section");
            }

            string displayName = user.UserName ?? "Khách hàng";
            if (displayName.Contains("@"))
            {
                var namePart = displayName.Split('@')[0];
                if (!string.IsNullOrEmpty(namePart))
                    displayName = char.ToUpper(namePart[0]) + namePart.Substring(1);
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = user.Id,
                UserName = displayName,
                UserEmail = user.Email,
                Rating = rating,
                Title = title?.Trim(),
                Comment = comment.Trim(),
                IsVerifiedPurchase = true,
                IsApproved = true,
                HelpfulCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _db.ProductReviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Cảm ơn bạn! Đánh giá của bạn đã được đăng thành công.";
            return Redirect($"/Products/Detail/{productId}#reviews-section");
        }

        // POST: /Products/VoteHelpful
        [HttpPost]
        public async Task<IActionResult> VoteHelpful(int reviewId)
        {
            var r = await _db.ProductReviews.FindAsync(reviewId);
            if (r == null) return NotFound();

            r.HelpfulCount++;
            await _db.SaveChangesAsync();

            return Json(new { success = true, helpfulCount = r.HelpfulCount });
        }
    }
}
