using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public NewsController(ApplicationDbContext db) => _db = db;

        [HttpGet]
        [Route("Cam-Nang")]
        [Route("Tin-Tuc")]
        [Route("News")]
        public async Task<IActionResult> Index(string? topic, string? q)
        {
            // Tự động nạp các bài viết cẩm nang mẫu chuẩn thể thao nếu CSDL chưa có bài viết nào
            if (!await _db.NewsPosts.AnyAsync())
            {
                var samplePosts = new List<NewsPost>
                {
                    new NewsPost
                    {
                        Title = "Cẩm Nang Chọn Vợt Cầu Lông Toàn Diện Cho Người Mới Đến Bán Chuyên",
                        Slug = "cam-nang-chon-vot-cau-long-toan-dien",
                        Summary = "Phân tích tường tận cách đọc thông số 3U, 4U, điểm cân bằng Head-Heavy/Even/Head-Light và độ cứng thân vợt để chọn đúng cây vợt nâng tầm lối đánh của bạn.",
                        Content = @"<p class='lead'>Khi mới bắt đầu bước vào thế giới cầu lông hoặc đang tìm kiếm cây vợt nâng cấp thứ hai, đại đa số người chơi đều bị choáng ngợp bởi vô số thuật ngữ: 3U/4U, điểm cân bằng 295mm, thân cứng hay dẻo. Việc chọn sai vợt không những khiến bạn nhanh mỏi tay mà còn có nguy cơ gây viêm gân cổ tay, khuỷu tay.</p>
<h3>1. Trọng lượng vợt (U) – Đừng chọn theo cảm tính</h3>
<p>Thông số U thể hiện trọng lượng của vợt khi chưa căng dây và quấn cán:</p>
<ul>
    <li><strong>3U (85 - 89g):</strong> Dành cho người chơi có lực cổ tay khỏe, thường xuyên chơi thể thao hoặc VĐV. Đem lại lực đập rất nặng nhưng đòi hỏi thể lực tốt.</li>
    <li><strong>4U (80 - 84g):</strong> Trọng lượng quốc dân, phù hợp với 85% người chơi phong trào Việt Nam. Linh hoạt trong cả công và thủ.</li>
    <li><strong>5U (75 - 79g):</strong> Siêu nhẹ, lý tưởng cho phái nữ hoặc người chơi thích lối đánh phản tạt nhanh trên lưới.</li>
</ul>
<h3>2. Điểm cân bằng (Balance Point)</h3>
<p>Điểm cân bằng quyết định cây vợt thuộc trường phái công hay thủ:</p>
<ul>
    <li><strong>Dưới 288mm (Đầu nhẹ):</strong> Phòng thủ, phản tạt tốc độ, bắt lưới cực nhanh.</li>
    <li><strong>288mm - 295mm (Cân bằng):</strong> Công thủ toàn diện, chuyển đổi linh hoạt.</li>
    <li><strong>Trên 295mm (Đầu nặng):</strong> Chuyên công smash, trợ lực đập cắm sân.</li>
</ul>
<blockquote>Lời khuyên từ HLV Tiêu Đao: Nếu bạn mới chơi dưới 1 năm, hãy bắt đầu bằng một cây vợt 4U thân dẻo vừa phải, điểm cân bằng khoảng 290mm. Cây vợt này sẽ trợ lực tốt và tha thứ cho các cú đánh lệch tâm.</blockquote>",
                        ThumbnailUrl = "/img/banners/banner.jpg",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new NewsPost
                    {
                        Title = "Đan Cước 4 Nút Hay 2 Nút? Bao Nhiêu Kg Là Chuẩn Lực Tay Không Méo Khung?",
                        Slug = "dan-cuoc-4-nut-hay-2-nut-so-kg-chuan",
                        Summary = "Tại sao quy chuẩn quốc tế của Yonex, Victor bắt buộc đan 4 nút? Lực cổ tay yếu nên căng bao nhiêu kg để trợ lực tốt và không gây chấn thương cổ tay?",
                        Content = @"<p class='lead'>Căng cước là công đoạn quyết định tới 40% cảm giác đánh cầu của bạn. Tuy nhiên, rất nhiều người chơi mắc sai lầm khi yêu cầu thợ căng mức kg quá cao so với lực tay thực tế.</p>
<h3>1. Vì sao nên căng cước máy điện tử 4 nút tiêu chuẩn Yonex?</h3>
<p>Kiểu đan 4 nút (Yonex 4-Knot Standard) chia dây dọc và dây ngang thành hai sợi riêng biệt với 4 nút thắt:</p>
<ul>
    <li>Dây dọc được căng trước đều từ tâm sang hai bên, giữ cho khung vợt không bị biến dạng.</li>
    <li>Dây ngang được căng sau với mức tải cao hơn dây dọc 1 - 2 lbs (khoảng 0.5kg) để bù trừ độ co giãn, tạo bề mặt lưới căng vuông góc hoàn hảo.</li>
    <li>Nếu chẳng may đứt một sợi trên sân, khung vợt vẫn giữ được cấu trúc cân bằng, không bị nứt gãy do lực kéo lệch một phía như kiểu đan 2 nút.</li>
</ul>
<h3>2. Bảng mức căng cước phù hợp theo lực cổ tay:</h3>
<ul>
    <li><strong>9.0kg - 10.0kg (20 - 22 lbs):</strong> Người mới chơi, học sinh, sinh viên, phái nữ. Trợ lực nảy cầu tốt nhất, tiếng nổ êm.</li>
    <li><strong>10.5kg - 11.5kg (23 - 25 lbs):</strong> Người chơi phong trào từ 1 - 3 năm, cổ tay khá. Cảm giác cầu tốt, đường cầu bay chính xác.</li>
    <li><strong>12.0kg trở lên (26+ lbs):</strong> Bán chuyên và chuyên nghiệp. Diện tích sweet-spot thu nhỏ, đòi hỏi kỹ thuật tiếp xúc cầu chuẩn xác.</li>
</ul>",
                        ThumbnailUrl = "/img/banners/banner1.jfif",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-3)
                    },
                    new NewsPost
                    {
                        Title = "Top 5 Mẫu Giày Cầu Lông Đệm Êm Giảm Chấn Bảo Vệ Khớp Tốt Nhất 2026",
                        Slug = "top-5-mau-giay-cau-long-dem-em-giam-chan-2026",
                        Summary = "Đánh giá chi tiết các công nghệ đệm Power Cushion+ của Yonex, EnergyMax 3.0 của Victor giúp bảo vệ tối đa khớp gối và cổ chân khi tiếp đất.",
                        Content = @"<p class='lead'>Trong một trận cầu lông kéo dài 1 tiếng, một tay vợt trung bình thực hiện hơn 300 cú bật nhảy và chuyển hướng đột ngột. Nếu đôi giày của bạn thiếu khả năng giảm chấn, toàn bộ phản lực sẽ truyền thẳng lên khớp gối và cột sống.</p>
<h3>1. Công nghệ Power Cushion+ (Yonex)</h3>
<p>Công nghệ độc quyền huyền thoại của Yonex cho phép quả trứng rơi từ độ cao 12 mét xuống tấm đệm nảy ngược lên 6 mét mà không vỡ. Các mẫu giày tiêu biểu: <strong>Yonex 65Z3, Eclipsion Z3</strong>.</p>
<h3>2. Công nghệ EnergyMax 3.0 (Victor)</h3>
<p>Đem lại cảm giác bám sàn và bật nảy siêu nhạy. Các mẫu giày như <strong>Victor P9200, A970 Nitro</strong> được các tay vợt đôi nam nữ hàng đầu thế giới tin dùng.</p>
<h3>3. Form chân bè (Wide Fit) – Điểm cần chú ý của người Việt</h3>
<p>Hơn 60% người Việt có bàn chân dạng bè ngang (Wide). Hãy ưu tiên các phiên bản có ký hiệu <em>3E (Tiêu chuẩn)</em> hoặc <em>4E (Chân bè rộng)</em> để tránh bị đau ngón chân út và phồng rộp khi thi đấu.</p>",
                        ThumbnailUrl = "/img/banners/banner2.jfif",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-6)
                    },
                    new NewsPost
                    {
                        Title = "Kỹ Thuật Đập Cầu Smash Uy Lực: 4 Bước Cải Thiện Tốc Độ Cầu Đáng Kể",
                        Slug = "ky-thuat-dap-cau-smash-uy-luc-4-buoc",
                        Summary = "Khám phá cách phát lực từ chân, hông, vai đến cổ tay để tạo ra những cú đập cầu cắm sân không thể chống đỡ từ các VĐV chuyên nghiệp.",
                        Content = @"<p class='lead'>Nhiều người chơi thường nghĩ rằng đập cầu mạnh là do sức mạnh cơ bắp tay. Tuy nhiên, bí mật của cú đập cầu xé gió 400km/h nằm ở chuỗi động năng (Kinetic Chain) hoàn chỉnh.</p>
<h3>Bước 1: Tư thế chuẩn bị và góc đón cầu</h3>
<p>Đứng nghiêng người so với lưới khoảng 45 độ, chân thuận đặt lùi về sau. Trọng tâm dồn vào chân sau để sẵn sàng bật nhảy hoặc đạp đất.</p>
<h3>Bước 2: Mở vai và giương vợt</h3>
<p>Mở rộng khớp vai, khuỷu tay nâng cao ngang tai. Đầu vợt thả lỏng ra sau lưng giống như kéo dây cung.</p>
<h3>Bước 3: Đạp đất xoay hông và vung vợt</h3>
<p>Lực truyền từ bàn chân đạp đất, xoay trục hông sang trái, sau đó dẫn lực lên vai, khuỷu tay và cuối cùng gập cổ tay (Pronation) vào thời điểm tiếp xúc cầu ở điểm cao nhất phía trước trán.</p>
<h3>Bước 4: Thu vợt theo quán tính</h3>
<p>Không ghìm vợt đột ngột. Hãy để vợt quét chéo qua hông bên trái theo quán tính để giải phóng lực và bảo vệ khớp khuỷu tay.</p>",
                        ThumbnailUrl = "/img/banners/banner3.jfif",
                        IsPublished = true,
                        PublishedAt = DateTime.UtcNow.AddDays(-9)
                    }
                };

                _db.NewsPosts.AddRange(samplePosts);
                await _db.SaveChangesAsync();
            }

            var query = _db.NewsPosts.Where(x => x.IsPublished).AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(x => x.Title.ToLower().Contains(term) || (x.Summary != null && x.Summary.ToLower().Contains(term)));
            }

            // Lọc theo chủ đề
            if (!string.IsNullOrWhiteSpace(topic))
            {
                var t = topic.Trim().ToLower();
                query = t switch
                {
                    "vot" => query.Where(x => x.Title.ToLower().Contains("vợt") || (x.Summary != null && x.Summary.ToLower().Contains("vợt"))),
                    "ky-thuat" => query.Where(x => x.Title.ToLower().Contains("kỹ thuật") || x.Title.ToLower().Contains("smash") || x.Title.ToLower().Contains("đập")),
                    "giay" => query.Where(x => x.Title.ToLower().Contains("giày") || x.Title.ToLower().Contains("size") || x.Title.ToLower().Contains("chân")),
                    "cang-cuoc" => query.Where(x => x.Title.ToLower().Contains("cước") || x.Title.ToLower().Contains("căng") || x.Title.ToLower().Contains("đan")),
                    _ => query
                };
            }

            var posts = await query.OrderByDescending(x => x.PublishedAt).ToListAsync();

            ViewBag.Topic = topic;
            ViewBag.Query = q;

            return View(posts);
        }

        [HttpGet]
        [Route("Cam-Nang/{id:int}")]
        [Route("News/Detail/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _db.NewsPosts.FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);
            if (post == null) return NotFound();

            // Bài viết trước và tiếp theo
            var prevPost = await _db.NewsPosts
                .Where(x => x.IsPublished && x.PublishedAt < post.PublishedAt)
                .OrderByDescending(x => x.PublishedAt)
                .FirstOrDefaultAsync();

            var nextPost = await _db.NewsPosts
                .Where(x => x.IsPublished && x.PublishedAt > post.PublishedAt)
                .OrderBy(x => x.PublishedAt)
                .FirstOrDefaultAsync();

            // 3 bài viết liên quan
            var relatedPosts = await _db.NewsPosts
                .Where(x => x.Id != id && x.IsPublished)
                .OrderByDescending(x => x.PublishedAt)
                .Take(3)
                .ToListAsync();

            // Top cẩm nang đọc nhiều nhất (Trending)
            var trendingPosts = await _db.NewsPosts
                .Where(x => x.Id != id && x.IsPublished)
                .OrderBy(x => x.Id)
                .Take(4)
                .ToListAsync();

            // Gợi ý sản phẩm chính hãng nổi bật đi kèm trong bài
            var recommendedProducts = await _db.Products
                .Include(p => p.Brand)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(3)
                .ToListAsync();

            ViewBag.RelatedPosts = relatedPosts;
            ViewBag.PrevPost = prevPost;
            ViewBag.NextPost = nextPost;
            ViewBag.TrendingPosts = trendingPosts;
            ViewBag.RecommendedProducts = recommendedProducts;

            return View(post);
        }
    }
}
