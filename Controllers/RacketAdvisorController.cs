using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    [Route("Tu-Van-Chon-Vot")]
    public class RacketAdvisorController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RacketAdvisorController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? playStyle, string? wristPower, string? budget)
        {
            ViewBag.PlayStyle = playStyle ?? "";
            ViewBag.WristPower = wristPower ?? "";
            ViewBag.Budget = budget ?? "";

            List<Product> recommendations = new();
            string adviceSummary = "";

            // Nếu người dùng đã chọn tiêu chí trắc nghiệm
            if (!string.IsNullOrWhiteSpace(playStyle) || !string.IsNullOrWhiteSpace(budget))
            {
                var allRackets = await _db.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p => p.Category.Slug == "vot" && p.IsActive)
                    .ToListAsync();

                var scoredRackets = allRackets.Select(r => new
                {
                    Racket = r,
                    Score = CalculateScore(r, playStyle, wristPower, budget)
                })
                .OrderByDescending(x => x.Score)
                .Take(4)
                .Select(x => x.Racket)
                .ToList();

                recommendations = scoredRackets;

                // Chuẩn bị lời phân tích chuyên gia
                if (playStyle == "smash")
                {
                    adviceSummary = "Dựa trên lối đánh tấn công uy lực của bạn, hệ thống khuyên dùng các dòng vợt đầu đầm (Head Heavy), thân cứng trung bình để tối ưu độ cắm và lực phát ra khi smash.";
                }
                else if (playStyle == "speed")
                {
                    adviceSummary = "Với lối đánh phản tạt điều cầu tốc độ, hệ thống gợi ý các mẫu vợt nhẹ đầu (Head Light) có khung vát khí động học xé gió, giúp xoay trở cực nhanh trên lưới và thủ cầu linh hoạt.";
                }
                else
                {
                    adviceSummary = "Lối đánh công thủ toàn diện đòi hỏi cây vợt có điểm cân bằng chuẩn (Even Balance), hỗ trợ kiểm soát cầu chính xác và chuyển đổi trạng thái công - thủ mượt mà.";
                }
            }

            ViewBag.AdviceSummary = adviceSummary;
            return View(recommendations);
        }

        private static int CalculateScore(Product r, string? playStyle, string? wristPower, string? budget)
        {
            int score = 0;
            var name = r.Name.ToLower();
            var price = r.SalePrice ?? r.Price;

            // 1. Phù hợp lối đánh
            if (playStyle == "smash")
            {
                if (name.Contains("astrox") || name.Contains("axforce") || name.Contains("thruster") || name.Contains("calibar"))
                    score += 50;
            }
            else if (playStyle == "speed")
            {
                if (name.Contains("nanoflare") || name.Contains("auraspeed") || name.Contains("jetspeed") || name.Contains("tectonic"))
                    score += 50;
            }
            else if (playStyle == "balanced")
            {
                if (name.Contains("arcsaber") || name.Contains("drivex") || name.Contains("fortius") || name.Contains("king"))
                    score += 50;
            }

            // 2. Phù hợp ngân sách
            if (budget == "low")
            {
                if (price < 2000000) score += 30;
                else score -= 20;
            }
            else if (budget == "mid")
            {
                if (price >= 2000000 && price <= 3800000) score += 30;
            }
            else if (budget == "pro")
            {
                if (price > 3500000) score += 30;
            }

            // 3. Phù hợp lực cổ tay
            if (wristPower == "beginner")
            {
                // Người mới: ưu tiên vợt giá vừa phải, trợ lực tốt
                if (price <= 3000000) score += 20;
            }
            else if (wristPower == "advanced")
            {
                // Lực khỏe: ưu tiên các dòng cao cấp Pro
                if (price >= 3500000) score += 20;
            }

            return score;
        }
    }
}
