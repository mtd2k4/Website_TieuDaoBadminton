using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Controllers
{
    public class RankingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public RankingController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("Bang-Xep-Hang")]
        [Route("Van-Dong-Vien")]
        [Route("Rankings")]
        public async Task<IActionResult> Index(string? region = null, string? brand = null, string? q = null)
        {
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync();

            var allPlayers = GetFullPlayerDatabase();

            // Tự động ánh xạ vợt của VĐV sang sản phẩm đang bán tại Shop
            foreach (var player in allPlayers)
            {
                var matchedProd = FindMatchingProduct(products, player.ShopMatchKeyword);
                if (matchedProd != null)
                {
                    player.ShopProductId = matchedProd.Id;
                    player.ShopProductName = matchedProd.Name;
                    player.ShopProductPrice = matchedProd.SalePrice ?? matchedProd.Price;
                }

                foreach (var hist in player.RacketHistory)
                {
                    var matchedHist = FindMatchingProduct(products, hist.RacketModel);
                    if (matchedHist != null)
                    {
                        hist.ShopProductId = matchedHist.Id;
                    }
                }
            }

            ViewBag.SelectedRegion = region ?? "all";
            ViewBag.SelectedBrand = brand ?? "all";
            ViewBag.SearchQuery = q ?? "";

            ViewBag.TotalVietnamPlayers = allPlayers.Count(p => p.RegionGroup == "Vietnam");
            ViewBag.TotalWorldPlayers = allPlayers.Count(p => p.RegionGroup == "World");
            ViewBag.TotalLegendPlayers = allPlayers.Count(p => p.RegionGroup == "Legends");

            var filtered = allPlayers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(region) && !string.Equals(region, "all", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p => string.Equals(p.RegionGroup, region, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(brand) && !string.Equals(brand, "all", StringComparison.OrdinalIgnoreCase))
            {
                filtered = filtered.Where(p =>
                    string.Equals(p.MainBrand, brand, StringComparison.OrdinalIgnoreCase) ||
                    p.RacketHistory.Any(h => string.Equals(h.Brand, brand, StringComparison.OrdinalIgnoreCase)));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var kw = q.Trim();
                filtered = filtered.Where(p =>
                    p.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    p.Nickname.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    p.Country.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    p.CurrentRacket.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                    p.RacketHistory.Any(h => h.RacketModel.Contains(kw, StringComparison.OrdinalIgnoreCase)));
            }

            return View(filtered.ToList());
        }

        private static Product? FindMatchingProduct(List<Product> products, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return null;

            var exact = products.FirstOrDefault(p => p.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // Thử khớp theo các từ khóa dòng vợt phổ biến trong Shop
            string[] knownModels =
            {
                "Astrox 99 Pro", "Astrox 88D Pro", "Nanoflare 1000Z", "Arcsaber 11 Pro",
                "Astrox 77", "Nanoflare 800", "Thruster K F", "Auraspeed 90K",
                "DriveX 9X", "Brave Sword 12", "Jetspeed S12", "Tectonic 9",
                "Axforce 90", "Calibar 900", "Tectonic 7", "Axforce 60",
                "Fortius 11", "Altius 01"
            };

            foreach (var model in knownModels)
            {
                if (keyword.Contains(model, StringComparison.OrdinalIgnoreCase))
                {
                    return products.FirstOrDefault(p => p.Name.Contains(model, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (keyword.Contains("Astrox 100ZZ", StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains("Astrox 99", StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains("Voltric Z-Force", StringComparison.OrdinalIgnoreCase))
            {
                return products.FirstOrDefault(p => p.Name.Contains("Astrox 99 Pro", StringComparison.OrdinalIgnoreCase));
            }

            if (keyword.Contains("Arcsaber 11", StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains("Duora 10", StringComparison.OrdinalIgnoreCase))
            {
                return products.FirstOrDefault(p => p.Name.Contains("Arcsaber 11 Pro", StringComparison.OrdinalIgnoreCase));
            }

            if (keyword.Contains("Thruster", StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains("TK-F", StringComparison.OrdinalIgnoreCase) ||
                keyword.Contains("Ryuga", StringComparison.OrdinalIgnoreCase))
            {
                return products.FirstOrDefault(p => p.Name.Contains("Thruster K F", StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }

        private static List<PlayerRankingVM> GetFullPlayerDatabase()
        {
            return new List<PlayerRankingVM>
            {
                // ==========================================================
                // PHẦN 1: BẢNG XẾP HẠNG VẬN ĐỘNG VIÊN VIỆT NAM TIÊU BIỂU
                // ==========================================================
                new PlayerRankingVM
                {
                    Rank = 1,
                    RegionGroup = "Vietnam",
                    Category = "Đơn Nữ (WS)",
                    FullName = "Nguyễn Thùy Linh",
                    Nickname = "Hoa khôi Cầu lông Việt Nam • Số 1 Đơn Nữ VN",
                    Country = "Việt Nam (Đồng Nai)",
                    CountryBadge = "VN",
                    BwfPoints = 49560,
                    BestWorldRank = "Hạng #20 BWF Thế Giới",
                    Achievements = "2 lần dự Olympic (Tokyo 2020, Paris 2024) • Á quân German Open Super 300 • 3 lần Vô địch Vietnam Open Super 100",
                    PlayStyle = "Tay phải • Điều cầu cổ tay biến ảo, gài lưới xoáy và đập cầu điểm rơi",
                    CurrentRacket = "Yonex Astrox 77 Pro (High Orange 4U-G5)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex Exbolt 63 / Aerobite • 11.5 kg (25.5 lbs)",
                    ShoeModel = "Yonex Power Cushion 65Z3 Trắng Cam",
                    ShopMatchKeyword = "Astrox 77",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2023 – Hiện tại", RacketModel = "Yonex Astrox 77 Pro (High Orange)", Brand = "Yonex", HighlightNote = "Sử dụng tại Olympic Paris 2024, Chung kết German Open 2024 & China Masters." },
                        new() { Period = "2021 – 2022", RacketModel = "Yonex Astrox 77 (Shine Red & Metallic Blue)", Brand = "Yonex", HighlightNote = "Thi đấu thăng hoa tại Olympic Tokyo 2020, SEA Games 31 và vô địch Vietnam Open 2022." },
                        new() { Period = "2018 – 2020", RacketModel = "Li-Ning Aeronaut 7000i / Yonex Voltric 70 E-Tune", Brand = "Li-Ning", HighlightNote = "Giai đoạn thống trị giải Vô địch Cá nhân Quốc gia và vô địch Bangladesh International." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 2,
                    RegionGroup = "Vietnam",
                    Category = "Đơn Nam (MS)",
                    FullName = "Nguyễn Tiến Minh",
                    Nickname = "Huyền thoại bất tử Cầu lông Việt Nam",
                    Country = "Việt Nam (TP. Hồ Chí Minh)",
                    CountryBadge = "VN",
                    BwfPoints = 68520,
                    BestWorldRank = "Hạng #5 BWF Thế Giới (Đỉnh cao)",
                    Achievements = "HCĐ Vô Địch Thế Giới 2013 • 4 lần liên tiếp dự Olympic (2008, 2012, 2016, 2020) • Kỷ lục 16 lần liên tiếp Vô địch Quốc gia",
                    PlayStyle = "Tay phải • Bộ pháp di chuyển bền bỉ, phòng thủ phản công và điều cầu 4 góc sân đỉnh cao",
                    CurrentRacket = "Yonex Astrox 99 Pro (Cherry Sunburst) & Mizuno Fortius 11",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG65 Titanium • 12.0 kg (26.5 lbs)",
                    ShoeModel = "Yonex SHB 65Z3 / Mizuno Wave Claw 2",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2022 – Hiện tại", RacketModel = "Yonex Astrox 99 Pro (Đỏ Cherry Sunburst) & Astrox 100ZZ", Brand = "Yonex", HighlightNote = "Trợ lực tối đa cho các pha smash dọc biên ở tuổi ngoài 40 tại các giải Quốc gia." },
                        new() { Period = "2019 – 2021", RacketModel = "Mizuno Fortius 11 Power / Mizuno Fortius 10 Quick", Brand = "Mizuno", HighlightNote = "Đại sứ thương hiệu Mizuno, tham dự Olympic Tokyo 2020 và giành HCĐ Châu Á 2019." },
                        new() { Period = "2015 – 2018", RacketModel = "Yonex Voltric Z-Force II (VTZF2) & Yonex Duora 10", Brand = "Yonex", HighlightNote = "Đánh bại Lin Dan/Huyền thoại khu vực và thi đấu rực lửa tại Olympic Rio 2016." },
                        new() { Period = "2010 – 2014 (Top 5 BWF)", RacketModel = "Yonex Voltric Z-Force (2012) & Yonex Arcsaber 11", Brand = "Yonex", HighlightNote = "Vũ khí lịch sử giúp Tiến Minh vươn lên Hạng 5 Thế giới và giành HCĐ Thế giới 2013." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 3,
                    RegionGroup = "Vietnam",
                    Category = "Đơn Nam (MS)",
                    FullName = "Lê Đức Phát",
                    Nickname = "Số 1 Đơn Nam Việt Nam Hiện Tại • Tuyển thủ Quân Đội",
                    Country = "Việt Nam (Quân Đội)",
                    CountryBadge = "VN",
                    BwfPoints = 26840,
                    BestWorldRank = "Hạng #62 BWF Thế Giới",
                    Achievements = "Đại diện Đơn nam Việt Nam dự Olympic Paris 2024 • Vô địch Uganda International 2024 • Á quân Kazakhstan International",
                    PlayStyle = "Tay phải (Cao 1m80) • Tấn công áp đảo, đập cầu uy lực từ cuối sân",
                    CurrentRacket = "Yonex Astrox 99 Pro / Yonex Astrox 100ZZ (Kurenai)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG80 Power • 13.0 kg (28.5 lbs)",
                    ShoeModel = "Yonex Power Cushion Eclipsion Z",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2023 – Hiện tại", RacketModel = "Yonex Astrox 100ZZ Kurenai & Yonex Astrox 99 Pro", Brand = "Yonex", HighlightNote = "Đạt chuỗi thắng quốc tế, giành vé chính thức dự Thế vận hội Olympic Paris 2024." },
                        new() { Period = "2020 – 2022", RacketModel = "Yonex Astrox 88D Pro & Victor Thruster Ryuga", Brand = "Yonex", HighlightNote = "Lên ngôi vô địch các giải Quốc gia và giải các cây vợt xuất sắc toàn quốc." },
                        new() { Period = "2017 – 2019", RacketModel = "Yonex Voltric Z-Force II", Brand = "Yonex", HighlightNote = "Vô địch Pakistan International Series 2017." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 4,
                    RegionGroup = "Vietnam",
                    Category = "Đơn Nam (MS)",
                    FullName = "Nguyễn Hải Đăng",
                    Nickname = "Momota Việt Nam • Tay vợt trẻ số 1 TP.HCM",
                    Country = "Việt Nam (TP. Hồ Chí Minh)",
                    CountryBadge = "VN",
                    BwfPoints = 24150,
                    BestWorldRank = "Hạng #75 BWF Thế Giới",
                    Achievements = "Vô địch Croatia Open • Vô địch Cyprus International • HCV Cá nhân trẻ Quốc gia",
                    PlayStyle = "Tay trái • Lối đánh kiểm soát cầu bền bỉ, bỏ nhỏ sát lưới tương đồng Kento Momota",
                    CurrentRacket = "Yonex Astrox 99 Pro (White Tiger) & Arcsaber 11 Pro",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex Aerobite • 12.5 kg (27.5 lbs)",
                    ShoeModel = "Yonex SHB 65Z3 Momota",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2022 – Hiện tại", RacketModel = "Yonex Astrox 99 Pro (White Tiger) & Yonex Arcsaber 11 Pro", Brand = "Yonex", HighlightNote = "Bứt phá vào Top 75 BWF Thế giới và thay thế vị trí chủ lực đơn nam của TP.HCM." },
                        new() { Period = "2019 – 2021", RacketModel = "Yonex Astrox 99 (Sunshine Orange & Sapphire Navy)", Brand = "Yonex", HighlightNote = "Sử dụng trọn bộ combo vợt Astrox 99 cam/xanh giống thần tượng Kento Momota." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 5,
                    RegionGroup = "Vietnam",
                    Category = "Đơn Nữ (WS)",
                    FullName = "Vũ Thị Trang",
                    Nickname = "Cựu Số 1 Đơn Nữ Việt Nam • Tuyển thủ Olympic Rio 2016",
                    Country = "Việt Nam (Bắc Giang / TP.HCM)",
                    CountryBadge = "VN",
                    BwfPoints = 36400,
                    BestWorldRank = "Hạng #34 BWF Thế Giới",
                    Achievements = "HCĐ Olympic Trẻ 2010 • HCĐ SEA Games 2013 & 2015 • Dự Olympic Rio 2016 • Vô địch Vietnam International",
                    PlayStyle = "Tay phải • Di chuyển dẻo dai, cắt cầu chéo sân cực kỳ khó chịu",
                    CurrentRacket = "Mizuno Altius 01 & Yonex Astrox 77",
                    MainBrand = "Mizuno",
                    StringAndTension = "Yonex BG65 Ti / Mizuno MT65 • 11.0 kg (24.5 lbs)",
                    ShoeModel = "Mizuno Wave Claw 2",
                    ShopMatchKeyword = "Altius 01",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2019 – Hiện tại", RacketModel = "Mizuno Altius 01 Feel & Mizuno Fortius 11", Brand = "Mizuno", HighlightNote = "Đồng hành cùng thương hiệu Mizuno giành nhiều danh hiệu International Series." },
                        new() { Period = "2017 – 2018", RacketModel = "Yonex Astrox 77 & Yonex Nanoflare 800", Brand = "Yonex", HighlightNote = "Lọt vào chung kết Vietnam Open." },
                        new() { Period = "2013 – 2016", RacketModel = "Yonex Arcsaber 11 & Yonex Nanoray 900", Brand = "Yonex", HighlightNote = "Giai đoạn thi đấu xuất sắc tại Olympic Rio 2016 và giành HCĐ SEA Games." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 6,
                    RegionGroup = "Vietnam",
                    Category = "Đôi Nam Nữ (XD)",
                    FullName = "Phạm Văn Hải & Thân Vân Anh",
                    Nickname = "Đôi Nam Nữ Số 1 Việt Nam",
                    Country = "Việt Nam (Bắc Giang)",
                    CountryBadge = "VN",
                    BwfPoints = 21300,
                    BestWorldRank = "Hạng #78 BWF Đôi Nam Nữ",
                    Achievements = "Nhiều năm liền HCV Đôi Nam Nữ Vô địch Quốc gia • Tuyển thủ Quốc gia dự SEA Games & ASIAD",
                    PlayStyle = "Phối hợp Công sau (Văn Hải) – Bắt lưới thần tốc (Vân Anh)",
                    CurrentRacket = "Yonex Astrox 88D Pro (Văn Hải) & Nanoflare 800 (Vân Anh)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG80 (12.5 kg) & Exbolt 63 (11.5 kg)",
                    ShoeModel = "Yonex SHB 65Z3 / Aerus Z",
                    ShopMatchKeyword = "Astrox 88D Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2022 – Hiện tại", RacketModel = "Yonex Astrox 88D Pro (Cầu sau) & Yonex Nanoflare 800 (Cầu trước)", Brand = "Yonex", HighlightNote = "Bộ đôi vũ khí chuyên dụng cho đánh đôi giúp thống trị nội dung Đôi Nam Nữ Quốc gia." },
                        new() { Period = "2019 – 2021", RacketModel = "Li-Ning Tectonic 7 & Victor Brave Sword 12", Brand = "Victor", HighlightNote = "Giai đoạn định hình lối chơi phản tạt nhanh trên lưới." }
                    }
                },

                // ==========================================================
                // PHẦN 2: BẢNG XẾP HẠNG BWF THẾ GIỚI HIỆN TẠI (WORLD TOP)
                // ==========================================================
                new PlayerRankingVM
                {
                    Rank = 1,
                    RegionGroup = "World",
                    Category = "Đơn Nam (MS)",
                    FullName = "Shi Yu Qi (Thạch Vũ Kỳ)",
                    Nickname = "Số 1 BWF Đơn Nam Thế Giới • Vua Chém Cầu",
                    Country = "Trung Quốc",
                    CountryBadge = "CN",
                    BwfPoints = 102415,
                    BestWorldRank = "Hạng #1 BWF Thế Giới",
                    Achievements = "Vô địch All England • Vô địch World Tour Finals 2024 • HCV Thomas Cup & Sudirman Cup • Á quân VĐTG",
                    PlayStyle = "Tay phải • Đập cầu nửa lực (Half-smash) và chém cầu giấu tay hiểm hóc nhất thế giới",
                    CurrentRacket = "Yonex Astrox 100ZZ (Dark Navy 4U-G5) / Astrox 99 Pro",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG80 Power • 14.0 kg (31 lbs)",
                    ShoeModel = "Yonex Power Cushion 65Z3 Trắng",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2021 – Hiện tại", RacketModel = "Yonex Astrox 100ZZ (Dark Navy & Kurenai)", Brand = "Yonex", HighlightNote = "Đưa Shi Yu Qi lên ngôi Số 1 BWF Thế giới năm 2024 với chuỗi vô địch French Open, India Open, Singapore Open." },
                        new() { Period = "2019 – 2020", RacketModel = "Li-Ning Aeronaut 9000C & Li-Ning Tectonic 9", Brand = "Li-Ning", HighlightNote = "Giai đoạn Đội tuyển Trung Quốc ký hợp đồng tài trợ cùng hãng Li-Ning." },
                        new() { Period = "2017 – 2018", RacketModel = "Yonex Astrox 77 (Xanh Chuối) & Yonex Duora Z-Strike", Brand = "Yonex", HighlightNote = "Đánh bại Lin Dan để vô địch All England 2018 và vô địch World Tour Finals 2018." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 2,
                    RegionGroup = "World",
                    Category = "Đơn Nam (MS)",
                    FullName = "Viktor Axelsen",
                    Nickname = "Đại Đế Đan Mạch • 2 Lần HCV Olympic Liên Tiếp",
                    Country = "Đan Mạch",
                    CountryBadge = "DK",
                    BwfPoints = 98790,
                    BestWorldRank = "Hạng #1 BWF Thế Giới (183 tuần)",
                    Achievements = "2 HCV Olympic (Tokyo 2020 & Paris 2024) • 2 lần Vô Địch Thế Giới (2017, 2022) • 5 lần Vô địch World Tour Finals",
                    PlayStyle = "Tay phải (Cao 1m94) • Đập cầu góc cắm sấm sét và phòng thủ sải tay khổng lồ",
                    CurrentRacket = "Yonex Astrox 100ZZ (Kurenai / VA Edition) & Astrox 99 Pro",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex Aerobite / BG80 • 14.5 kg (32 lbs)",
                    ShoeModel = "Yonex Power Cushion 65Z3",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2020 – Hiện tại", RacketModel = "Yonex Astrox 100ZZ (Dark Navy & Kurenai)", Brand = "Yonex", HighlightNote = "Thống trị tuyệt đối làng cầu lông thế giới: Giành 2 HCV Olympic 2020 & 2024, VĐTG 2022." },
                        new() { Period = "2018 – 2019", RacketModel = "Yonex Duora Z-Strike & Yonex Astrox 88S", Brand = "Yonex", HighlightNote = "Tăng tốc độ phản tạt và kiểm soát nửa sân trước." },
                        new() { Period = "2015 – 2017", RacketModel = "Yonex Arcsaber 11 (Đỏ Trắng)", Brand = "Yonex", HighlightNote = "Giành HCĐ Olympic Rio 2016 và lên ngôi Vô Địch Thế Giới 2017 sau khi thắng Lin Dan." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 3,
                    RegionGroup = "World",
                    Category = "Đơn Nam (MS)",
                    FullName = "Anders Antonsen",
                    Nickname = "Ngôi Sao Số 2 Đan Mạch • Nhà Vô Địch Châu Âu",
                    Country = "Đan Mạch",
                    CountryBadge = "DK",
                    BwfPoints = 91620,
                    BestWorldRank = "Hạng #2 BWF Thế Giới",
                    Achievements = "2 lần Vô địch Châu Âu • Vô địch Malaysia Open Super 1000 • Vô địch World Tour Finals • HCB Thế giới 2019",
                    PlayStyle = "Tay phải • Đánh lừa cổ tay trên lưới và tấn công biến hóa nhịp độ",
                    CurrentRacket = "Victor Auraspeed 90K II / Auraspeed 90K Metallic",
                    MainBrand = "Victor",
                    StringAndTension = "Victor VBS-66 Nano • 13.5 kg (30 lbs)",
                    ShoeModel = "Victor A970 Nitrolite",
                    ShopMatchKeyword = "Auraspeed 90K",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2021 – Hiện tại", RacketModel = "Victor Auraspeed 90K (ARS-90K) & Auraspeed 90K Metallic", Brand = "Victor", HighlightNote = "Gương mặt đại diện toàn cầu của dòng Victor Auraspeed 90K, vô địch Malaysia Open & Denmark Open." },
                        new() { Period = "2018 – 2020", RacketModel = "Li-Ning Aeronaut 9000C & Li-Ning Tectonic 7", Brand = "Li-Ning", HighlightNote = "Giành HCB Thế giới 2019 và Vô địch BWF World Tour Finals 2020." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 4,
                    RegionGroup = "World",
                    Category = "Đơn Nam (MS)",
                    FullName = "Kunlavut Vitidsarn",
                    Nickname = "Thần Đồng Cầu Lông Thái Lan • Nhà Vô Địch Thế Giới",
                    Country = "Thái Lan",
                    CountryBadge = "TH",
                    BwfPoints = 88540,
                    BestWorldRank = "Hạng #3 BWF Thế Giới",
                    Achievements = "Vô Địch Thế Giới 2023 • HCB Olympic Paris 2024 • Kỷ lục 3 lần liên tiếp Vô địch Trẻ Thế giới (2017, 2018, 2019)",
                    PlayStyle = "Tay phải • Phòng thủ như tường đồng vách sắt và phản công điều cầu cực kỳ lì lợm",
                    CurrentRacket = "Yonex Astrox 88D Pro (Camel Gold / Silver) & Nanoflare 1000Z",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG66 Ultimax • 13.0 kg (28.5 lbs)",
                    ShoeModel = "Yonex Power Cushion 65Z3",
                    ShopMatchKeyword = "Astrox 88D Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2021 – Hiện tại", RacketModel = "Yonex Astrox 88D Pro & Yonex Nanoflare 1000Z", Brand = "Yonex", HighlightNote = "Dùng Nanoflare 1000Z & Astrox 88D Pro đăng quang Vô Địch Thế Giới 2023 và giành HCB Olympic Paris 2024." },
                        new() { Period = "2019 – 2020", RacketModel = "Yonex Astrox 88S & Yonex Astrox 77", Brand = "Yonex", HighlightNote = "Lập kỷ lục 3 lần vô địch Trẻ Thế giới liên tiếp." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 5,
                    RegionGroup = "World",
                    Category = "Đơn Nam (MS)",
                    FullName = "Lee Zii Jia",
                    Nickname = "Vua Đập Cầu Trái Tay (Backhand Smash) Malaysia",
                    Country = "Malaysia",
                    CountryBadge = "MY",
                    BwfPoints = 85310,
                    BestWorldRank = "Hạng #2 BWF Thế Giới",
                    Achievements = "HCĐ Olympic Paris 2024 • Vô địch All England 2021 • Vô địch Châu Á 2022 • HCV SEA Games",
                    PlayStyle = "Tay phải • Lối đánh tấn công bạo liệt, cú đập cầu thuận tay & trái tay uy lực bậc nhất BWF",
                    CurrentRacket = "Victor Thruster Ryuga Metallic / Victor Thruster K F (TK-F)",
                    MainBrand = "Victor",
                    StringAndTension = "Victor VBS-66 Nano • 14.0 kg (31 lbs)",
                    ShoeModel = "Victor A970ACE / P9200",
                    ShopMatchKeyword = "Thruster K F",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2022 – Hiện tại", RacketModel = "Victor Thruster Ryuga / Ryuga II Pro / Thruster K F", Brand = "Victor", HighlightNote = "Đại sứ thương hiệu độc quyền của dòng Victor Thruster, giành Vô địch Châu Á 2022 & HCĐ Olympic Paris 2024." },
                        new() { Period = "2020 – 2021", RacketModel = "Yonex Astrox 99 (Cam) & Yonex Astrox 100ZZ", Brand = "Yonex", HighlightNote = "Đánh bại cả Momota và Axelsen để lên ngôi vô địch All England 2021." },
                        new() { Period = "2018 – 2019", RacketModel = "Victor Thruster K F (TK-F Đen Vàng) & Thruster BXR", Brand = "Victor", HighlightNote = "Giành HCV Đơn nam SEA Games 2019." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 6,
                    RegionGroup = "World",
                    Category = "Đơn Nam (MS)",
                    FullName = "Loh Kean Yew",
                    Nickname = "Tia Chớp Singapore • Cựu Vô Địch Thế Giới",
                    Country = "Singapore",
                    CountryBadge = "SG",
                    BwfPoints = 76200,
                    BestWorldRank = "Hạng #3 BWF Thế Giới",
                    Achievements = "Vô Địch Thế Giới 2021 • Vô địch Spain Masters 2024 • Á quân Giải Vô địch Châu Á",
                    PlayStyle = "Tay phải • Tốc độ di chuyển nhanh nhất thế giới, bật nhảy đập cầu góc rộng",
                    CurrentRacket = "Li-Ning Axforce 90 (Dragon Max) & Li-Ning Tectonic 9",
                    MainBrand = "Li-Ning",
                    StringAndTension = "Li-Ning No.1 • 13.5 kg (30 lbs)",
                    ShoeModel = "Li-Ning AYTT 009 Pro",
                    ShopMatchKeyword = "Axforce 90",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2023 – Hiện tại", RacketModel = "Li-Ning Axforce 90 (Dragon Max) & Axforce 80", Brand = "Li-Ning", HighlightNote = "Cây vợt thiên công đầu nặng giúp Loh Kean Yew tối ưu hóa tốc độ smash." },
                        new() { Period = "2020 – 2021", RacketModel = "Li-Ning Tectonic 9 (4U) & Li-Ning Calibar 900", Brand = "Li-Ning", HighlightNote = "Tạo địa chấn lịch sử khi vô địch Giải Vô Địch Thế Giới BWF 2021 tại Tây Ban Nha." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 7,
                    RegionGroup = "World",
                    Category = "Đơn Nữ (WS)",
                    FullName = "An Se Young",
                    Nickname = "Độc Cô Cầu Bại Đơn Nữ Thế Giới • HCV Olympic 2024",
                    Country = "Hàn Quốc",
                    CountryBadge = "KR",
                    BwfPoints = 111317,
                    BestWorldRank = "Hạng #1 BWF Đơn Nữ Thế Giới",
                    Achievements = "HCV Olympic Paris 2024 • Vô Địch Thế Giới 2023 • HCV ASIAD 19 • 10 danh hiệu Super Series trong 1 năm",
                    PlayStyle = "Tay phải • Thể lực vô tận, bộ pháp di chuyển không góc chết và điều cầu chuẩn từng milimet",
                    CurrentRacket = "Yonex Astrox 77 Pro (High Orange 4U-G5)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG80 • 12.5 kg (27.5 lbs)",
                    ShoeModel = "Yonex Aerus Z / Comfort Z3",
                    ShopMatchKeyword = "Astrox 77",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2023 – Hiện tại", RacketModel = "Yonex Astrox 77 Pro (High Orange)", Brand = "Yonex", HighlightNote = "Giành trọn bộ HCV Olympic Paris 2024, HCV ASIAD và Vô Địch Thế Giới 2023." },
                        new() { Period = "2018 – 2022", RacketModel = "Yonex Astrox 77 (Shine Red & Shine Yellow)", Brand = "Yonex", HighlightNote = "Gắn bó trung thành tuyệt đối với dòng vợt Astrox 77 từ năm 16 tuổi đến khi lên đỉnh thế giới." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 8,
                    RegionGroup = "World",
                    Category = "Đơn Nữ (WS)",
                    FullName = "Chen Yu Fei (Trần Vũ Phi)",
                    Nickname = "Nhà Vô Địch Olympic Tokyo 2020",
                    Country = "Trung Quốc",
                    CountryBadge = "CN",
                    BwfPoints = 101680,
                    BestWorldRank = "Hạng #1 BWF Đơn Nữ Thế Giới",
                    Achievements = "HCV Olympic Tokyo 2020 • Vô địch All England • 3 lần vô địch Uber Cup & Sudirman Cup",
                    PlayStyle = "Tay phải • Công thủ toàn diện, tâm lý thi đấu bản lĩnh và ít đánh hỏng nhất BWF",
                    CurrentRacket = "Yonex Astrox 77 Pro & Yonex Nanoflare 800",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex Exbolt 63 / BG80 • 12.5 kg (27.5 lbs)",
                    ShoeModel = "Yonex SHB 65Z3",
                    ShopMatchKeyword = "Astrox 77",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2022 – Hiện tại", RacketModel = "Yonex Astrox 77 Pro & Yonex Astrox 77", Brand = "Yonex", HighlightNote = "Vũ khí chủ lực giúp Chen Yu Fei duy trì vị trí Top 2 Đơn nữ thế giới suốt 4 năm liền." },
                        new() { Period = "2019 – 2021", RacketModel = "Li-Ning Aeronaut 9000 & Yonex Astrox 77 Đỏ", Brand = "Li-Ning", HighlightNote = "Đăng quang HCV Đơn nữ Thế vận hội Olympic Tokyo 2020." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 9,
                    RegionGroup = "World",
                    Category = "Đơn Nữ (WS)",
                    FullName = "Tai Tzu Ying (Đài Tư Dĩnh)",
                    Nickname = "Nữ Hoàng Kỹ Thuật & Cổ Tay Ảo Diệu BWF",
                    Country = "Đài Bắc Trung Hoa",
                    CountryBadge = "TW",
                    BwfPoints = 94850,
                    BestWorldRank = "Hạng #1 BWF Thế Giới (Kỷ lục 214 tuần)",
                    Achievements = "HCB Olympic Tokyo 2020 • 3 lần Vô địch All England • 4 lần Vô địch World Tour Finals • HCV ASIAD",
                    PlayStyle = "Tay phải • Bậc thầy giả động tác (Deception), cổ tay lỏng và gài cầu chéo lưới",
                    CurrentRacket = "Victor Thruster K F (TK-F Enhanced / TK-FC)",
                    MainBrand = "Victor",
                    StringAndTension = "Victor VBS-66 Nano • 13.0 kg (29 lbs)",
                    ShoeModel = "Victor P9200TTY Signature",
                    ShopMatchKeyword = "Thruster K F",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2021 – Hiện tại", RacketModel = "Victor Thruster F Claw (TK-FC) & Thruster K F Enhanced", Brand = "Victor", HighlightNote = "Giành HCB Olympic Tokyo 2020, HCB Thế giới 2021 và Vô địch World Tour Finals 2023." },
                        new() { Period = "2017 – 2020", RacketModel = "Victor Thruster K Falcon (TK-F Đen Vàng)", Brand = "Victor", HighlightNote = "Cây vợt huyền thoại đưa Tai Tzu Ying giữ ngôi Số 1 thế giới 214 tuần kỷ lục." },
                        new() { Period = "2014 – 2016", RacketModel = "Victor Thruster K 7000S & Brave Sword 12", Brand = "Victor", HighlightNote = "Vô địch Super Series Finals Dubai." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 10,
                    RegionGroup = "World",
                    Category = "Đôi Nam (MD)",
                    FullName = "Lee Yang & Wang Chi-Lin",
                    Nickname = "Cặp Đôi Vàng Lịch Sử • 2 Lần HCV Olympic Liên Tiếp",
                    Country = "Đài Bắc Trung Hoa",
                    CountryBadge = "TW",
                    BwfPoints = 89400,
                    BestWorldRank = "Hạng #2 BWF Đôi Nam Thế Giới",
                    Achievements = "Cặp đôi nam duy nhất lịch sử giành 2 HCV Olympic liên tiếp (Tokyo 2020 & Paris 2024)",
                    PlayStyle = "Phản tạt áp sát lưới tốc độ siêu thanh (Lee Yang) & Pháo đài đập cầu cuối sân (Wang Chi-Lin)",
                    CurrentRacket = "Yonex Nanoflare 800 (Lee Yang) & Nanoflare 1000Z (Wang Chi-Lin)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG80 • 14.0 kg (31 lbs)",
                    ShoeModel = "Yonex Power Cushion 65Z3",
                    ShopMatchKeyword = "Nanoflare 1000Z",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2023 – Paris 2024", RacketModel = "Yonex Nanoflare 800 Pro (Lee Yang) & Yonex Nanoflare 1000Z (Wang Chi-Lin)", Brand = "Yonex", HighlightNote = "Toàn thắng 7 trận liên tiếp để bảo vệ thành công tấm HCV Olympic Paris 2024." },
                        new() { Period = "2020 – Tokyo 2020", RacketModel = "Yonex Nanoflare 800 & Yonex Astrox 88D Pro", Brand = "Yonex", HighlightNote = "Giành HCV Olympic Tokyo 2020 và 3 giải Super 1000 tại Thái Lan." },
                        new() { Period = "2017 – 2019", RacketModel = "Victor Brave Sword 12 (BS12) & Victor Jetspeed S12", Brand = "Victor", HighlightNote = "Giai đoạn khoác áo đội tuyển tài trợ bởi Victor." }
                    }
                },

                // ==========================================================
                // PHẦN 3: HUYỀN THOẠI CẦU LÔNG THẾ GIỚI (LEGENDS OF BWF)
                // ==========================================================
                new PlayerRankingVM
                {
                    Rank = 1,
                    RegionGroup = "Legends",
                    Category = "Đơn Nam (MS)",
                    FullName = "Lin Dan (Lâm Đan - Super Dan)",
                    Nickname = "GOAT • Tay Vợt Vĩ Đại Nhất Lịch Sử Cầu Lông",
                    Country = "Trung Quốc",
                    CountryBadge = "CN",
                    BwfPoints = 109500,
                    BestWorldRank = "Hạng #1 BWF Thế Giới (Full Super Grand Slam)",
                    Achievements = "2 HCV Olympic (2008, 2012) • 5 lần Vô Địch Thế Giới • 6 lần Vô địch All England • 2 HCV ASIAD",
                    PlayStyle = "Tay trái • Kiểm soát nhịp độ trận đấu thượng thừa và cú Jump Smash chéo sân bất khả chiến bại",
                    CurrentRacket = "Yonex Voltric LD-Force / Li-Ning Woods N90",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG80 • 14.0 kg (31 lbs)",
                    ShoeModel = "Yonex SHB 03Z / Comfort Z",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2015 – 2020", RacketModel = "Yonex Voltric LD-Force & Voltric Z-Force II LD", Brand = "Yonex", HighlightNote = "Phiên bản chữ ký độc quyền của Yonex dành riêng cho Lin Dan, vô địch All England 2016 & Malaysia Open 2019." },
                        new() { Period = "2009 – 2014", RacketModel = "Li-Ning Woods N90 (N90 Đỏ Thế hệ 1)", Brand = "Li-Ning", HighlightNote = "Cây vợt huyền thoại quấn khăn trắng nửa cán gỗ giúp Lin Dan giành HCV Olympic London 2012 & VĐTG 2011, 2013." },
                        new() { Period = "2004 – 2008", RacketModel = "Yonex Armortec 700 (AT700 Đỏ)", Brand = "Yonex", HighlightNote = "Huỷ diệt mọi đối thủ để giành HCV Olympic Bắc Kinh 2008." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 2,
                    RegionGroup = "Legends",
                    Category = "Đơn Nam (MS)",
                    FullName = "Lee Chong Wei",
                    Nickname = "Ông Vua Super Series • 349 Tuần Giữ Ngôi Số 1 BWF",
                    Country = "Malaysia",
                    CountryBadge = "MY",
                    BwfPoints = 108800,
                    BestWorldRank = "Hạng #1 BWF Thế Giới (349 tuần liên tiếp & tích lũy)",
                    Achievements = "3 HCB Olympic (2008, 2012, 2016) • 46 danh hiệu Super Series • 4 lần Vô địch All England • 12 lần Vô địch Malaysia Open",
                    PlayStyle = "Tay phải • Bộ pháp lò xo nhanh nhất lịch sử và cú phòng thủ bung cầu phản công siêu tưởng",
                    CurrentRacket = "Yonex Astrox 99 LCW & Yonex Duora 10 LCW",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex BG66 Ultimax • 13.5 kg (30 lbs)",
                    ShoeModel = "Yonex SHB 65Z / Aerus LCW",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2016 – 2019", RacketModel = "Yonex Duora 10 LCW & Yonex Astrox 99 LCW", Brand = "Yonex", HighlightNote = "Vợt 2 mặt Box/Aero giúp Lee Chong Wei đánh bại Lin Dan tại Bán kết Olympic Rio 2016 và vô địch Malaysia Open lần thứ 12." },
                        new() { Period = "2012 – 2015", RacketModel = "Yonex Voltric Z-Force (VTZF) & Voltric Z-Force II LCW", Brand = "Yonex", HighlightNote = "Thống trị bảng xếp hạng BWF suốt 4 năm với dòng vợt nặng đầu siêu cứng." },
                        new() { Period = "2008 – 2011", RacketModel = "Yonex Armortec 900 Power (AT900P)", Brand = "Yonex", HighlightNote = "Giành HCB Olympic Bắc Kinh 2008 và vô địch All England 2010, 2011." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 3,
                    RegionGroup = "Legends",
                    Category = "Đơn Nam (MS)",
                    FullName = "Kento Momota",
                    Nickname = "Kỷ Lục Gia Guinness • 11 Chức Vô Địch Trong Năm 2019",
                    Country = "Nhật Bản",
                    CountryBadge = "JP",
                    BwfPoints = 111918,
                    BestWorldRank = "Hạng #1 BWF Thế Giới (Kỷ lục điểm số cao nhất lịch sử)",
                    Achievements = "2 lần Vô Địch Thế Giới (2018, 2019) • Vô địch All England 2019 • Kỷ lục Guinness 11 danh hiệu năm 2019 (Tỷ lệ thắng 90.4%)",
                    PlayStyle = "Tay trái • Kỹ thuật bỏ nhỏ sát mép lưới hoàn hảo và điều cầu làm kiệt sức mọi đối thủ",
                    CurrentRacket = "Yonex Astrox 99 Pro (White Tiger & Cherry Sunburst)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex Aerobite • 14.0 kg (31 lbs)",
                    ShoeModel = "Yonex Power Cushion 65Z3 KM",
                    ShopMatchKeyword = "Astrox 99 Pro",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2021 – 2024", RacketModel = "Yonex Astrox 99 Pro (White Tiger & Cherry Sunburst)", Brand = "Yonex", HighlightNote = "Được Yonex đồng thiết kế cùng Momota với mặt vợt mở rộng ISOMETRIC và gen liền trợ lực." },
                        new() { Period = "2018 – 2020 (Đỉnh cao Guinness)", RacketModel = "Yonex Astrox 99 (Sunshine Orange & Sapphire Navy)", Brand = "Yonex", HighlightNote = "Cây vợt biểu tượng giúp Momota giành 11 danh hiệu trong năm 2019 và 2 chức Vô Địch Thế Giới." },
                        new() { Period = "2015 – 2018", RacketModel = "Yonex Astrox 88D & Yonex Voltric Z-Force II", Brand = "Yonex", HighlightNote = "Vô địch Super Series Finals 2015 và Vô Địch Thế Giới lần đầu năm 2018." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 4,
                    RegionGroup = "Legends",
                    Category = "Đơn Nam (MS)",
                    FullName = "Chen Long (Trần Long)",
                    Nickname = "Vạn Lý Trường Thành Trung Quốc • HCV Olympic Rio 2016",
                    Country = "Trung Quốc",
                    CountryBadge = "CN",
                    BwfPoints = 102000,
                    BestWorldRank = "Hạng #1 BWF Thế Giới (76 tuần liên tiếp)",
                    Achievements = "Đủ bộ 3 Huy chương Olympic (HCV Rio 2016, HCB Tokyo 2020, HCĐ London 2012) • 2 lần Vô Địch Thế Giới (2014, 2015)",
                    PlayStyle = "Tay phải (Cao 1m88) • Phòng thủ kín kẽ không kẽ hở và đập cầu dọc dây cực nặng",
                    CurrentRacket = "Li-Ning Tectonic 9 & Li-Ning Axforce 80",
                    MainBrand = "Li-Ning",
                    StringAndTension = "Li-Ning No.1 • 14.0 kg (31 lbs)",
                    ShoeModel = "Li-Ning AYTT 009",
                    ShopMatchKeyword = "Tectonic 9",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2020 – 2022", RacketModel = "Li-Ning Tectonic 9 & Li-Ning Axforce 80", Brand = "Li-Ning", HighlightNote = "Vào tới Chung kết và giành HCB Olympic Tokyo 2020." },
                        new() { Period = "2016 – 2019", RacketModel = "Li-Ning 3D Calibar 900C & Aeronaut 7000B", Brand = "Li-Ning", HighlightNote = "Đánh bại Lee Chong Wei để đăng quang HCV Olympic Rio 2016." },
                        new() { Period = "2012 – 2015", RacketModel = "Li-Ning Flame N55 & Li-Ning N99", Brand = "Li-Ning", HighlightNote = "2 năm liên tiếp lên ngôi Vô Địch Thế Giới 2014 và 2015." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 5,
                    RegionGroup = "Legends",
                    Category = "Đơn Nữ (WS)",
                    FullName = "Carolina Marin",
                    Nickname = "Nữ Chiến Binh Tây Ban Nha • 3 Lần Vô Địch Thế Giới",
                    Country = "Tây Ban Nha",
                    CountryBadge = "ES",
                    BwfPoints = 93700,
                    BestWorldRank = "Hạng #1 BWF Đơn Nữ Thế Giới",
                    Achievements = "HCV Olympic Rio 2016 • 3 lần Vô Địch Thế Giới (2014, 2015, 2018) • 7 lần liên tiếp Vô địch Châu Âu • 2 lần Vô địch All England",
                    PlayStyle = "Tay trái • Tốc độ vung vợt nhanh nhất làng đơn nữ, lối đánh tấn công dồn dập áp đảo",
                    CurrentRacket = "Yonex Nanoflare 1000Z (Lightning Yellow)",
                    MainBrand = "Yonex",
                    StringAndTension = "Yonex Aerobite • 13.5 kg (30 lbs)",
                    ShoeModel = "Yonex Aerus Z Nữ",
                    ShopMatchKeyword = "Nanoflare 1000Z",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2023 – Hiện tại", RacketModel = "Yonex Nanoflare 1000Z (Vàng Đen)", Brand = "Yonex", HighlightNote = "Vô địch All England 2024, Swiss Open 2024 và lọt vào Bán kết Olympic Paris 2024." },
                        new() { Period = "2018 – 2022", RacketModel = "Yonex Duora Z-Strike & Yonex Nanoflare 800", Brand = "Yonex", HighlightNote = "Đăng quang Vô Địch Thế Giới lần thứ 3 năm 2018." },
                        new() { Period = "2014 – 2017", RacketModel = "Yonex Nanoray Z-Speed (NRZS)", Brand = "Yonex", HighlightNote = "Giành HCV Olympic Rio 2016 và 2 chức VĐTG 2014, 2015." }
                    }
                },
                new PlayerRankingVM
                {
                    Rank = 6,
                    RegionGroup = "Legends",
                    Category = "Đôi Nam (MD)",
                    FullName = "Hendra Setiawan & Mohammad Ahsan",
                    Nickname = "The Daddies • Huyền Thoại Đôi Nam Indonesia",
                    Country = "Indonesia",
                    CountryBadge = "ID",
                    BwfPoints = 84600,
                    BestWorldRank = "Hạng #1 BWF Đôi Nam Thế Giới",
                    Achievements = "Hendra HCV Olympic 2008 • Cùng nhau 3 lần Vô Địch Thế Giới (2013, 2015, 2019) • 2 lần Vô địch All England",
                    PlayStyle = "Đẳng cấp bắt lưới phản xạ thiên tài (Hendra) & Đập cầu liên hoàn cuối sân (Ahsan)",
                    CurrentRacket = "Victor DriveX 9X (Hendra) & Victor Auraspeed 90K (Ahsan)",
                    MainBrand = "Victor",
                    StringAndTension = "Victor VBS-68 • 13.5 kg (30 lbs)",
                    ShoeModel = "Victor P9200 / A970",
                    ShopMatchKeyword = "DriveX 9X",
                    RacketHistory = new List<RacketHistoryItemVM>
                    {
                        new() { Period = "2021 – Hiện tại", RacketModel = "Victor DriveX 9X / 10X (Hendra) & Victor Auraspeed 90K / 100X (Ahsan)", Brand = "Victor", HighlightNote = "Lọt vào Chung kết All England và World Tour Finals ở tuổi gần 40." },
                        new() { Period = "2019 – 2020", RacketModel = "Mizuno Fortius 10 Quick (Hendra) & Mizuno Fortius 11 Power (Ahsan)", Brand = "Mizuno", HighlightNote = "Mùa giải thần kỳ: Vô địch cả 3 giải lớn nhất năm (All England, Vô Địch Thế Giới, World Tour Finals 2019)." },
                        new() { Period = "2013 – 2016", RacketModel = "Yonex Nanoray 900 (Hendra) & Yonex Arcsaber FB / Nanoray 800 (Ahsan)", Brand = "Yonex", HighlightNote = "Vô Địch Thế Giới 2013 và 2015." }
                    }
                }
            };
        }
    }
}

