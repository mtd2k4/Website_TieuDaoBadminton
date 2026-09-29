using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Models;

namespace tieudaobadminton.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Đảm bảo bảng ContactMessages & StockReceipts tồn tại nếu chưa chạy migration
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ContactMessages')
BEGIN
    CREATE TABLE [ContactMessages] (
        [Id] int NOT NULL IDENTITY,
        [FullName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        [Subject] nvarchar(max) NULL,
        [Message] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [IsReplied] bit NOT NULL,
        [StaffNote] nvarchar(max) NULL,
        CONSTRAINT [PK_ContactMessages] PRIMARY KEY ([Id])
    );
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'StockReceipts')
BEGIN
    CREATE TABLE [StockReceipts] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [ProductVariantId] int NULL,
        [QuantityAdded] int NOT NULL,
        [PreviousStock] int NOT NULL,
        [NewStock] int NOT NULL,
        [SupplierName] nvarchar(max) NOT NULL DEFAULT '',
        [OriginCountry] nvarchar(max) NULL,
        [BatchNumber] nvarchar(max) NULL,
        [UnitImportPrice] decimal(18,2) NOT NULL DEFAULT 0,
        [TotalImportCost] decimal(18,2) NOT NULL DEFAULT 0,
        [Note] nvarchar(max) NULL,
        [CreatedBy] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_StockReceipts] PRIMARY KEY ([Id])
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceipts' AND COLUMN_NAME = 'SupplierName')
        ALTER TABLE [StockReceipts] ADD [SupplierName] nvarchar(max) NOT NULL DEFAULT '';
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceipts' AND COLUMN_NAME = 'OriginCountry')
        ALTER TABLE [StockReceipts] ADD [OriginCountry] nvarchar(max) NULL;
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceipts' AND COLUMN_NAME = 'BatchNumber')
        ALTER TABLE [StockReceipts] ADD [BatchNumber] nvarchar(max) NULL;
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceipts' AND COLUMN_NAME = 'UnitImportPrice')
        ALTER TABLE [StockReceipts] ADD [UnitImportPrice] decimal(18,2) NOT NULL DEFAULT 0;
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StockReceipts' AND COLUMN_NAME = 'TotalImportCost')
        ALTER TABLE [StockReceipts] ADD [TotalImportCost] decimal(18,2) NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProductReviews')
BEGIN
    CREATE TABLE [ProductReviews] (
        [Id] int NOT NULL IDENTITY,
        [ProductId] int NOT NULL,
        [UserId] nvarchar(max) NULL,
        [UserName] nvarchar(100) NOT NULL,
        [UserEmail] nvarchar(100) NULL,
        [Rating] int NOT NULL,
        [Title] nvarchar(200) NULL,
        [Comment] nvarchar(max) NOT NULL,
        [IsVerifiedPurchase] bit NOT NULL,
        [IsApproved] bit NOT NULL,
        [HelpfulCount] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProductReviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ProductReviews_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
    );
END
");
            }
            catch { /* Không chặn luồng khởi động */ }

            // Seed đánh giá mẫu nếu bảng ProductReviews còn trống
            try
            {
                if (!await db.ProductReviews.AnyAsync() && await db.Products.AnyAsync())
                {
                    var prods = await db.Products.Take(25).ToListAsync();
                    var sampleReviews = new (string name, int rating, string title, string comment, int daysAgo)[]
                    {
                        ("Trần Quốc Bảo (Hà Nội)", 5, "Đánh cực kỳ thoát cầu, smash siêu cắm!", "Hàng chuẩn chính hãng tem phân phối đầy đủ, đã quét mã check chuẩn 100%. Cước đan chuẩn máy điện tử 11kg nảy tiếng nổ rất đã tai. Shop đóng hộp cẩn thận, giao hỏa tốc rất nhanh.", 1),
                        ("Nguyễn Minh Tuấn", 5, "Cây vợt tuyệt vời nhất từng trải nghiệm", "Đầm đầu vừa phải, xoay sở trên lưới cực kỳ thanh thoát. Rất hợp cho lối chơi công thủ toàn diện và cầu đôi. Quà tặng bao nhung và quấn cán rất cao cấp.", 3),
                        ("Lê Hoàng Nam (Đà Nẵng)", 4, "Chất lượng gia công đỉnh cao", "Màu sơn tinh xảo không tì vết. Đũa vợt trợ lực tốt, độ cứng đầm tay yêu cầu cổ tay trung bình trở lên để phát huy tối đa lực smash. Rất đáng đồng tiền!", 6),
                        ("Vũ Đức Thịnh", 5, "Dịch vụ của Tiêu Đao Badminton 10/10", "Được tư vấn mức đan cước rất nhiệt tình, kỹ thuật viên đan 4 nút không bị xô cước. Đóng gói bọc bóng khí 3 lớp chắc chắn. 5 sao ủng hộ shop dài dài.", 9),
                        ("Phạm Thanh Tùng (TP.HCM)", 5, "Trợ lực phông cầu và bỏ nhỏ rất êm tay", "Cảm giác cầm nắm chắc chắn, triệt tiêu rung chấn tốt nên sau 4 set thi đấu cường độ cao không hề bị mỏi vai hay nhức cổ tay.", 14)
                    };

                    foreach (var p in prods)
                    {
                        var reviewRnd = new Random(p.Id + 2026);
                        int count = reviewRnd.Next(3, 6);
                        for (int i = 0; i < count; i++)
                        {
                            var r = sampleReviews[i % sampleReviews.Length];
                            db.ProductReviews.Add(new ProductReview
                            {
                                ProductId = p.Id,
                                UserName = r.name,
                                Rating = r.rating,
                                Title = r.title,
                                Comment = r.comment,
                                IsVerifiedPurchase = true,
                                IsApproved = true,
                                HelpfulCount = reviewRnd.Next(2, 19),
                                CreatedAt = DateTime.UtcNow.AddDays(-r.daysAgo)
                            });
                        }
                    }
                    await db.SaveChangesAsync();
                }
            }
            catch { /* Không chặn luồng khởi động */ }

            // Tự động kiểm tra và chuẩn hóa toàn bộ dữ liệu Size & Màu sắc cho database hiện có
            try
            {
                await NormalizeProductVariantsAsync(db);
            }
            catch { /* Không chặn luồng khởi động */ }

            // Chỉ seed 1 lần
            if (await db.Categories.AnyAsync()) return;

            // ===== Categories =====
            var catVot = new Category { Name = "Vợt cầu lông", Slug = "vot" };
            var catGiay = new Category { Name = "Giày cầu lông", Slug = "giay" };
            var catAo = new Category { Name = "Quần áo cầu lông", Slug = "quan-ao" };
            db.Categories.AddRange(catVot, catGiay, catAo);

            // ===== Brands =====
            var yonex = new Brand { Name = "Yonex" };
            var victor = new Brand { Name = "Victor" };
            var lining = new Brand { Name = "Li-Ning" };
            var mizuno = new Brand { Name = "Mizuno" };
            var kawasaki = new Brand { Name = "Kawasaki" };
            db.Brands.AddRange(yonex, victor, lining, mizuno, kawasaki);

            await db.SaveChangesAsync();

            // Helper tạo sản phẩm
            Product NewProduct(string name, Category cat, Brand brand, decimal price, decimal? sale, string img)
                => new Product
                {
                    Name = name,
                    CategoryId = cat.Id,
                    BrandId = brand.Id,
                    Price = price,
                    SalePrice = sale,
                    MainImageUrl = img,
                    Description = "Hàng chính hãng, bảo hành đầy đủ. Giao nhanh toàn quốc.",
                    IsActive = true
                };

            // ===== 1) VỢT (20 sản phẩm, mỗi sản phẩm 1 variant stock) =====
            var rackets = new List<(string name, Brand brand, decimal price, decimal? sale)>
            {
                ("Astrox 99 Pro", yonex, 4500000, 3990000),
                ("Astrox 88D Pro", yonex, 4200000, 3790000),
                ("Nanoflare 1000Z", yonex, 5200000, 4890000),
                ("Arcsaber 11 Pro", yonex, 4300000, null),
                ("Thruster K F", victor, 3900000, 3490000),
                ("Auraspeed 90K", victor, 4100000, 3690000),
                ("DriveX 9X", victor, 3600000, null),
                ("Tectonic 9", lining, 3200000, 2990000),
                ("Axforce 90", lining, 3500000, 3190000),
                ("Calibar 900", lining, 2800000, null),
                ("Mizuno Fortius 11", mizuno, 3400000, 3090000),
                ("Mizuno Altius 01", mizuno, 2900000, null),
                ("Kawasaki Honor S6", kawasaki, 1500000, 1290000),
                ("Kawasaki King K9", kawasaki, 1800000, 1590000),
                ("Astrox 77", yonex, 2900000, 2690000),
                ("Nanoflare 800", yonex, 3200000, null),
                ("Brave Sword 12", victor, 2500000, 2290000),
                ("Jetspeed S12", victor, 2700000, null),
                ("Tectonic 7", lining, 2400000, 2190000),
                ("Axforce 60", lining, 2100000, null),
            };

            var rnd = new Random(2026);
            var racketImgs = Enumerable.Range(1, 12).Select(i => $"/img/products/rackets/r{i}.jfif").ToArray();
            var shoeImgs = Enumerable.Range(1, 12).Select(i => $"/img/products/shoes/s{i}.jfif").ToArray();
            var clothImgs = Enumerable.Range(1, 12).Select(i => $"/img/products/clothes/c{i}.jfif").ToArray();

            string Pick(string[] arr) => arr[rnd.Next(arr.Length)];


            foreach (var (name, brand, price, sale) in rackets)
            {
                var p = NewProduct($"{brand.Name} {name}", catVot, brand, price, sale, Pick(racketImgs));
                db.Products.Add(p);
                await db.SaveChangesAsync();

                var racketColors = GetRacketColorsForProduct(p.Name);
                var racketSizes = new[] { "4U-G5", "3U-G5" };
                for (int sIdx = 0; sIdx < racketSizes.Length; sIdx++)
                {
                    for (int cIdx = 0; cIdx < racketColors.Length; cIdx++)
                    {
                        db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = racketSizes[sIdx],
                            Color = racketColors[cIdx],
                            Stock = rnd.Next(6, 22),
                            Sku = $"RKT-{p.Id:D4}-{racketSizes[sIdx][..2]}-C{cIdx + 1}"
                        });
                    }
                }
            }

            await db.SaveChangesAsync();

            // ===== 2) GIÀY (20 sản phẩm, nhiều variant size + màu) =====
            var shoes = new List<(string name, Brand brand, decimal price, decimal? sale)>
            {
                ("SHB 65Z3", yonex, 2500000, 2290000),
                ("Comfort Z3", yonex, 2700000, null),
                ("Power Cushion Eclipsion Z", yonex, 2900000, 2690000),
                ("A970", victor, 2100000, null),
                ("A830", victor, 1900000, 1790000),
                ("P9200", victor, 3200000, 2990000),
                ("AYTT 005", lining, 1600000, 1490000),
                ("AYTT 007", lining, 1800000, null),
                ("AYTT 009", lining, 2200000, 1990000),
                ("Wave Claw 2", mizuno, 2300000, 2090000),
                ("Wave Fang Pro", mizuno, 2600000, null),
                ("Thunder Blade", mizuno, 1700000, 1590000),
                ("Kawasaki K-068", kawasaki, 1200000, 1090000),
                ("Kawasaki K-086", kawasaki, 1350000, null),
                ("SHB 03Z", yonex, 2400000, null),
                ("Aerus Z", yonex, 3100000, 2890000),
                ("S82", victor, 1650000, 1550000),
                ("A311", victor, 1400000, null),
                ("AYTT 011", lining, 2000000, null),
                ("K-099", kawasaki, 1450000, 1290000),
            };

            var shoeSizes = new[] { "38", "39", "40", "41", "42", "43", "44" };

            foreach (var (name, brand, price, sale) in shoes)
            {
                var p = NewProduct($"{brand.Name} {name}", catGiay, brand, price, sale, Pick(shoeImgs));
                db.Products.Add(p);
                await db.SaveChangesAsync();

                var shoeColors = GetShoeColorsForProduct(p.Name);
                for (int cIdx = 0; cIdx < shoeColors.Length; cIdx++)
                {
                    var color = shoeColors[cIdx];
                    foreach (var size in shoeSizes)
                    {
                        db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = size,
                            Color = color,
                            Stock = rnd.Next(5, 18),
                            Sku = $"SHO-{p.Id:D4}-{size}-C{cIdx + 1}"
                        });
                    }
                }
            }

            await db.SaveChangesAsync();

            // ===== 3) QUẦN ÁO (25 sản phẩm, variant size + màu) =====
            var clothes = new List<(string name, Brand brand, decimal price, decimal? sale)>
            {
                ("Áo thi đấu Pro Dry", yonex, 550000, 499000),
                ("Áo training basic", yonex, 390000, null),
                ("Áo CLB Limited", victor, 480000, 429000),
                ("Áo cổ bẻ Sport", victor, 520000, null),
                ("Áo thi đấu Lightning", lining, 460000, 399000),
                ("Áo training Air", lining, 420000, null),
                ("Quần short Pro", yonex, 420000, 379000),
                ("Quần short basic", victor, 350000, null),
                ("Váy cầu lông nữ", lining, 520000, 469000),
                ("Áo khoác warm-up", mizuno, 790000, 729000),
                ("Áo thun thể thao", mizuno, 450000, null),
                ("Áo đôi (cặp)", kawasaki, 520000, 459000),
                ("Quần dài training", yonex, 690000, null),
                ("Set áo + quần", victor, 890000, 799000),
                ("Áo thi đấu Team", lining, 520000, null),
                ("Áo polo premium", yonex, 650000, 599000),
                ("Quần short nữ", victor, 380000, 349000),
                ("Áo thun nữ", lining, 390000, null),
                ("Áo khoác gió", mizuno, 820000, null),
                ("Bộ training basic", kawasaki, 750000, 689000),
                ("Áo thun oversize", yonex, 430000, 399000),
                ("Áo cổ tròn Sport", victor, 410000, null),
                ("Quần short Speed", lining, 410000, 379000),
                ("Áo thi đấu Elite", mizuno, 560000, 519000),
                ("Áo CLB 2026", kawasaki, 420000, null),
            };

            var clothSizes = new[] { "S", "M", "L", "XL", "XXL" };
            var clothColors = new[] { "Đen", "Trắng", "Xanh Navy" };

            foreach (var (name, brand, price, sale) in clothes)
            {
                var p = NewProduct($"{brand.Name} {name}", catAo, brand, price, sale, Pick(clothImgs));

                db.Products.Add(p);
                await db.SaveChangesAsync();

                foreach (var size in clothSizes)
                    foreach (var color in clothColors)
                    {
                        db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = size,
                            Color = color,
                            Stock = rnd.Next(5, 20),
                            Sku = $"CLT-{p.Id:D4}-{size}-{color[..1].ToUpper()}"
                        });
                    }
            }

            await db.SaveChangesAsync();
            // ===== SEED BÁN CHẠY: 15 SP (5 vợt + 5 giày + 5 quần áo) =====
            var rackets15 = await db.Products.Include(p => p.Category)
                .Where(p => p.Category.Slug == "vot")
                .OrderByDescending(p => p.Id)
                .Take(5).ToListAsync();

            var shoes15 = await db.Products.Include(p => p.Category)
                .Where(p => p.Category.Slug == "giay")
                .OrderByDescending(p => p.Id)
                .Take(5).ToListAsync();

            var clothes15 = await db.Products.Include(p => p.Category)
                .Where(p => p.Category.Slug == "quan-ao")
                .OrderByDescending(p => p.Id)
                .Take(5).ToListAsync();

            var top15 = rackets15.Concat(shoes15).Concat(clothes15).ToList();

            // 1 đơn "Done" chứa đủ 15 SP => đảm bảo home hiển thị đủ 15
            var order = new Order
            {
                UserId = "seed",
                FullName = "Khách Seed",
                Phone = "0900000000",
                Address = "Việt Nam",
                Status = "Done"
            };
            db.Orders.Add(order);
            await db.SaveChangesAsync();

            decimal total = 0;
            foreach (var p in top15)
            {
                var qty = rnd.Next(2, 10); // mua nhiều để nổi lên
                var unit = p.SalePrice ?? p.Price;

                db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductName = p.Name,      // Home đang group theo ProductName
                    Quantity = qty,
                    UnitPrice = unit
                });

                total += unit * qty;
            }
            order.Total = total;
            await db.SaveChangesAsync();
        }

        private static readonly Dictionary<string, string[]> RacketColorsMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Astrox 99 Pro"] = new[] { "Trắng Bạch Hổ", "Đỏ Cherry" },
            ["Astrox 88D Pro"] = new[] { "Đen / Đỏ Camel", "Bạc / Đỏ Pro" },
            ["Nanoflare 1000Z"] = new[] { "Vàng Tia Chớp", "Đen / Vàng Lightning" },
            ["Arcsaber 11 Pro"] = new[] { "Xám / Đỏ", "Đỏ Ruby" },
            ["Thruster K F"] = new[] { "Đen / Xanh", "Trắng Claw" },
            ["Auraspeed 90K"] = new[] { "Xanh Đậm / Cam", "Trắng Băng Tuyết" },
            ["DriveX 9X"] = new[] { "Xanh Đen / Vàng", "Đen Huyền Bí" },
            ["Tectonic 9"] = new[] { "Trắng / Xanh Dương", "Đen Nhám" },
            ["Axforce 90"] = new[] { "Đen / Vàng Rồng", "Đỏ Hổ Phách" },
            ["Calibar 900"] = new[] { "Đỏ / Vàng Đồng", "Xám Titan" },
            ["Mizuno Fortius 11"] = new[] { "Đen / Bạc", "Đỏ Sunburst" },
            ["Mizuno Altius 01"] = new[] { "Trắng / Xanh Ngọc", "Hồng Pastel" },
            ["Kawasaki Honor S6"] = new[] { "Trắng / Tím", "Đen / Cam" },
            ["Kawasaki King K9"] = new[] { "Vàng Gold / Đen", "Xanh Neon" },
            ["Astrox 77"] = new[] { "Xanh Chuối", "Đỏ Shiny" },
            ["Nanoflare 800"] = new[] { "Đen Nhám / Vàng", "Xanh Lam" },
            ["Brave Sword 12"] = new[] { "Xanh Dương Cổ Điển", "Đỏ Bão Táp" },
            ["Jetspeed S12"] = new[] { "Xanh Lục Bảo", "Tím Huyền Ảo" },
            ["Tectonic 7"] = new[] { "Trắng Tuyết", "Xanh Biển Sâu" },
            ["Axforce 60"] = new[] { "Xanh Rêu / Đen", "Bạc Kim Loại" }
        };

        private static readonly Dictionary<string, string[]> ShoeColorsMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["SHB 65Z3"] = new[] { "Trắng / Đỏ", "Đen / Vàng" },
            ["Comfort Z3"] = new[] { "Trắng / Xanh", "Đen Toàn Diện" },
            ["Power Cushion Eclipsion Z"] = new[] { "Xanh Navy / Đỏ", "Đen / Vàng" },
            ["A970"] = new[] { "Đen / Vàng", "Trắng Ngọc" },
            ["A830"] = new[] { "Trắng / Xanh Dương", "Đỏ / Đen" },
            ["P9200"] = new[] { "Trắng / Xanh Navy", "Đen / Đỏ" },
            ["AYTT 005"] = new[] { "Trắng / Cam", "Đen / Xanh" },
            ["AYTT 007"] = new[] { "Xanh Dương / Trắng", "Đen / Bạc" },
            ["AYTT 009"] = new[] { "Trắng / Đỏ", "Đen Neon" },
            ["Wave Claw 2"] = new[] { "Trắng / Xanh Dương", "Đỏ / Đen" },
            ["Wave Fang Pro"] = new[] { "Trắng / Vàng Gold", "Đen / Đỏ" },
            ["Thunder Blade"] = new[] { "Xanh Navy / Cam", "Trắng / Đen" },
            ["Kawasaki K-068"] = new[] { "Trắng / Đỏ", "Xanh / Đen" },
            ["Kawasaki K-086"] = new[] { "Đen / Cam", "Trắng / Xanh" },
            ["SHB 03Z"] = new[] { "Đỏ / Đen", "Trắng / Xanh" },
            ["Aerus Z"] = new[] { "Xanh Cyan", "Xám Bạc" },
            ["S82"] = new[] { "Trắng / Đen", "Đỏ / Vàng" },
            ["A311"] = new[] { "Trắng / Xanh Dương", "Đen / Đỏ" },
            ["AYTT 011"] = new[] { "Trắng / Tím", "Đen / Xanh Lá" },
            ["K-099"] = new[] { "Đen / Vàng", "Trắng / Đỏ" }
        };

        public static string[] GetRacketColorsForProduct(string productName)
        {
            foreach (var kvp in RacketColorsMap)
            {
                if (productName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return new[] { "Đen / Đỏ", "Trắng / Xanh" };
        }

        public static string[] GetShoeColorsForProduct(string productName)
        {
            foreach (var kvp in ShoeColorsMap)
            {
                if (productName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return new[] { "Trắng / Đỏ", "Đen / Vàng" };
        }

        private static async Task NormalizeProductVariantsAsync(ApplicationDbContext db)
        {
            if (!await db.Products.AnyAsync()) return;

            var products = await db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .ToListAsync();

            var rnd = new Random(2026);
            bool hasChanges = false;

            foreach (var p in products)
            {
                var catSlug = p.Category?.Slug?.ToLower() ?? "";
                var pName = p.Name.ToLower();

                bool isRacket = catSlug == "vot" || catSlug.Contains("vot") || pName.Contains("vợt") || pName.Contains("astrox") || pName.Contains("nanoflare") || pName.Contains("arcsaber") || pName.Contains("axforce") || pName.Contains("halbertec") || pName.Contains("blade") || pName.Contains("drivex") || pName.Contains("auraspeed") || pName.Contains("thruster") || pName.Contains("tectonic") || pName.Contains("calibar");
                bool isShoe = catSlug == "giay" || catSlug.Contains("giay") || pName.Contains("giày") || pName.Contains("shb") || pName.Contains("aerus") || pName.Contains("eclipsion") || pName.Contains("wave claw") || pName.Contains("wave fang");
                bool isApparel = catSlug == "quan-ao" || catSlug.Contains("ao") || pName.Contains("áo") || pName.Contains("quần") || pName.Contains("váy") || pName.Contains("set");

                if (isRacket)
                {
                    var racketColors = GetRacketColorsForProduct(p.Name);
                    var racketSizes = new[] { "4U-G5", "3U-G5" };

                    // Kiểm tra xem vợt này có bị thiếu size hoặc màu không (ví dụ Size null hoặc Color null)
                    bool needsFix = p.Variants.Count == 0 ||
                                    p.Variants.Any(v => string.IsNullOrWhiteSpace(v.Size) || v.Size == "-" || string.IsNullOrWhiteSpace(v.Color) || v.Color == "-") ||
                                    p.Variants.Count < 4;

                    if (needsFix)
                    {
                        var existingVariants = p.Variants.ToList();
                        int variantIndex = 0;

                        foreach (var size in racketSizes)
                        {
                            for (int cIdx = 0; cIdx < racketColors.Length; cIdx++)
                            {
                                var color = racketColors[cIdx];
                                var existing = variantIndex < existingVariants.Count ? existingVariants[variantIndex] : null;

                                if (existing != null)
                                {
                                    existing.Size = size;
                                    existing.Color = color;
                                    if (existing.Stock <= 0) existing.Stock = rnd.Next(6, 20);
                                    existing.Sku = $"RKT-{p.Id:D4}-{size[..2]}-C{cIdx + 1}";
                                }
                                else
                                {
                                    db.ProductVariants.Add(new ProductVariant
                                    {
                                        ProductId = p.Id,
                                        Size = size,
                                        Color = color,
                                        Stock = rnd.Next(6, 20),
                                        Sku = $"RKT-{p.Id:D4}-{size[..2]}-C{cIdx + 1}"
                                    });
                                }
                                variantIndex++;
                            }
                        }
                        hasChanges = true;
                    }
                }
                else if (isShoe)
                {
                    var targetColors = GetShoeColorsForProduct(p.Name);
                    var targetSizes = new[] { "38", "39", "40", "41", "42", "43", "44" };

                    // Cập nhật các màu cũ như 'Trắng', 'Đen' đơn điệu sang phối màu sắc nét chuẩn
                    foreach (var v in p.Variants)
                    {
                        if (v.Color == "Trắng" || v.Color == "white")
                        {
                            v.Color = targetColors[0];
                            hasChanges = true;
                        }
                        else if (v.Color == "Đen" || v.Color == "black")
                        {
                            v.Color = targetColors.Length > 1 ? targetColors[1] : targetColors[0];
                            hasChanges = true;
                        }
                    }

                    // Bổ sung các size còn thiếu (như 38, 44) cho từng màu
                    foreach (var color in targetColors)
                    {
                        foreach (var size in targetSizes)
                        {
                            var exists = p.Variants.Any(v =>
                                string.Equals(v.Size?.Trim(), size, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(v.Color?.Trim(), color, StringComparison.OrdinalIgnoreCase));

                            if (!exists)
                            {
                                db.ProductVariants.Add(new ProductVariant
                                {
                                    ProductId = p.Id,
                                    Size = size,
                                    Color = color,
                                    Stock = rnd.Next(5, 18),
                                    Sku = $"SHO-{p.Id:D4}-{size}-{(color == targetColors[0] ? "C1" : "C2")}"
                                });
                                hasChanges = true;
                            }
                        }
                    }
                }
                else if (isApparel)
                {
                    var distinctColors = p.Variants
                        .Where(v => !string.IsNullOrWhiteSpace(v.Color) && v.Color != "-")
                        .Select(v => v.Color!.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (distinctColors.Count == 0)
                        distinctColors = new List<string> { "Đen", "Trắng", "Xanh Navy" };

                    // Đảm bảo có size XXL
                    foreach (var color in distinctColors)
                    {
                        var hasXXL = p.Variants.Any(v =>
                            string.Equals(v.Size?.Trim(), "XXL", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(v.Color?.Trim(), color, StringComparison.OrdinalIgnoreCase));

                        if (!hasXXL)
                        {
                            db.ProductVariants.Add(new ProductVariant
                            {
                                ProductId = p.Id,
                                Size = "XXL",
                                Color = color,
                                Stock = rnd.Next(6, 18),
                                Sku = $"CLT-{p.Id:D4}-XXL-{color[..1].ToUpper()}"
                            });
                            hasChanges = true;
                        }
                    }
                }
            }

            if (hasChanges)
            {
                await db.SaveChangesAsync();
            }
        }
    }
}
