using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;

namespace tieudaobadminton.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ChatbotController(ApplicationDbContext db)
        {
            _db = db;
        }

        public class ChatbotAskRequest
        {
            public string Message { get; set; } = "";
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] ChatbotAskRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Message))
            {
                return BadRequest(new { reply = "Vui lòng nhập câu hỏi của bạn." });
            }

            var userMsg = req.Message.Trim();

            // Lấy dữ liệu sản phẩm & tồn kho thực tế từ Database để tư vấn trực tiếp
            var products = await _db.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(30)
                .ToListAsync();

            var reply = BuildSmartDatabaseResponse(userMsg, products);
            return Ok(new
            {
                reply = reply,
                source = "local-ai"
            });
        }

        private static string BuildSmartDatabaseResponse(string input, List<Models.Product> products)
        {
            var q = input.ToLowerInvariant();

            var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3 && w != "với" && w != "cho" && w != "của" && w != "còn" && w != "shop")
                .ToList();

            var matchedProducts = products
                .Where(p => p.Name.ToLowerInvariant().Contains(q) ||
                            (p.Brand != null && q.Contains(p.Brand.Name.ToLowerInvariant())) ||
                            words.Any(w => p.Name.ToLowerInvariant().Contains(w)))
                .Take(3)
                .ToList();

            if (matchedProducts.Count > 0 && !q.Contains("căng cước") && !q.Contains("bảo hành") && !q.Contains("tra cứu"))
            {
                var sb = new StringBuilder("🏸 <strong>Sản phẩm tại kho phù hợp với câu hỏi của bạn:</strong><br/>");
                foreach (var mp in matchedProducts)
                {
                    var price = mp.SalePrice ?? mp.Price;
                    var stock = mp.Variants.Sum(v => v.Stock);
                    var variantsInfo = string.Join(", ", mp.Variants.Where(v => v.Stock > 0).Take(4).Select(v => $"{v.Color} {v.Size} ({v.Stock})".Trim()));
                    sb.Append($"• <strong>{mp.Name}</strong> — Giá: <span class='text-danger fw-bold'>{price:n0}đ</span><br/>");
                    sb.Append($"  <small class='text-muted'>Kho còn {stock} chiếc {(string.IsNullOrEmpty(variantsInfo) ? "" : $": {variantsInfo}")}</small><br/>");
                    sb.Append($"  <a href='/Products/Detail/{mp.Id}' class='chat-action-link'>Xem chi tiết & Chọn mua →</a><br/>");
                }
                return sb.ToString();
            }

            if (q.Contains("vợt") || q.Contains("người mới") || q.Contains("công thủ") || q.Contains("smash"))
            {
                var rackets = products.Where(p => p.Category?.Slug == "vot").Take(2).ToList();
                var racketLinks = string.Join("", rackets.Select(r =>
                    $"<br/>• <strong>{r.Name}</strong> ({(r.SalePrice ?? r.Price):n0}đ) <a href='/Products/Detail/{r.Id}' class='chat-action-link'>Xem vợt →</a>"));

                return $@"🏸 <strong>Tư vấn chọn vợt cầu lông chuẩn:</strong><br/>
                • <strong>Người mới / Phong trào:</strong> Nên chọn vợt <strong>4U (80-84g)</strong> cân bằng, dễ thuần.<br/>
                • <strong>Chuyên công Smash:</strong> Chọn vợt nặng đầu (Head-Heavy).{racketLinks}<br/>
                👉 <a href='/Tu-Van-Chon-Vot' class='chat-action-link'>Trắc nghiệm chọn vợt AI →</a>";
            }

            if (q.Contains("cước") || q.Contains("căng") || q.Contains("đan") || q.Contains("kg"))
            {
                return @"🛠️ <strong>Dịch vụ Căng Cước Điện Tử 4 Nút:</strong><br/>
                • <strong>Miễn phí công đan máy</strong> trị giá 60.000đ khi mua vợt tại shop!<br/>
                • <strong>Mức cân gợi ý:</strong> Người mới (9.5 - 10.5kg) • Phong trào (10.5 - 11.5kg) • Bán chuyên (12kg+).<br/>
                👉 <a href='/Dich-Vu-Cang-Cuoc' class='chat-action-link'>Xem bảng giá cước & quy trình đan 15p →</a>";
            }

            if (q.Contains("giày") || q.Contains("size") || q.Contains("chân") || q.Contains("áo") || q.Contains("quần"))
            {
                return @"👟 <strong>Hướng dẫn chọn Size Giày & Trang Phục:</strong><br/>
                • Đo chiều dài bàn chân (cm) và cộng thêm <strong>0.5cm</strong> (trừ hao tất dày thi đấu).<br/>
                • Bàn chân bè ngang nên ưu tiên dòng <strong>Wide (3E/4E)</strong>.<br/>
                👉 <a href='/Huong-Dan-Size' class='chat-action-link'>Mở bảng quy đổi Size chuẩn →</a>
                <a href='/Danh-Muc?cat=giay' class='chat-action-link'>Xem các mẫu giày giảm chấn →</a>";
            }

            if (q.Contains("đơn") || q.Contains("tra cứu") || q.Contains("vận chuyển") || q.Contains("giao hàng"))
            {
                return @"📦 <strong>Tra cứu đơn hàng & Vận chuyển:</strong><br/>
                • Giao siêu tốc nội thành <strong>2-4 giờ</strong>, toàn quốc 2-3 ngày.<br/>
                👉 <a href='/Tra-Cuu-Don-Hang' class='chat-action-link'>Tra cứu tiến độ đơn hàng ngay →</a>";
            }

            if (q.Contains("bảo hành") || q.Contains("freeship") || q.Contains("đổi trả") || q.Contains("chính sách") || q.Contains("ship"))
            {
                return @"🛡️ <strong>Chính sách Ưu đãi & Bảo hành:</strong><br/>
                • Cam kết <strong>100% Chính hãng</strong> (Yonex, Victor, Li-Ning, Mizuno, Kawasaki).<br/>
                • <strong>Freeship toàn quốc</strong> cho đơn hàng từ 499.000đ.<br/>
                • Đổi trả miễn phí trong <strong>7 ngày</strong> nếu có lỗi từ nhà sản xuất.";
            }

            if (q.Contains("liên hệ") || q.Contains("hotline") || q.Contains("nhân viên") || q.Contains("địa chỉ") || q.Contains("gọi") || q.Contains("sđt"))
            {
                return @"📞 <strong>Thông tin Liên hệ Tiêu Đao Badminton:</strong><br/>
                • <strong>Hotline Tư vấn 24/7:</strong> <a href='tel:0900000000' class='fw-bold text-danger'>0900 000 000</a><br/>
                • <strong>Địa chỉ:</strong> 123 Đường Cầu Lông, TP. Hồ Chí Minh (8:00 - 21:30)<br/>
                👉 <a href='tel:0900000000' class='chat-action-link'>📞 Gọi ngay cho kỹ thuật viên →</a>";
            }

            return $@"🏸 Xin chào! Với yêu cầu <strong>""{input}""</strong>, bạn có thể tham khảo:<br/>
            • <a href='/Tu-Van-Chon-Vot' class='chat-action-link'>⚡ Trợ lý Tư vấn Vợt AI</a>
            • <a href='/Danh-Muc' class='chat-action-link'>📂 Xem toàn bộ sản phẩm</a><br/>
            • Hoặc gọi trực tiếp Hotline <a href='tel:0900000000' class='fw-bold text-danger'>0900 000 000</a> để kỹ thuật viên tư vấn 1-1 ngay nhé!";
        }
    }
}
