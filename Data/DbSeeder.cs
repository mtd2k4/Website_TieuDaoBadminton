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

            // Đảm bảo bảng ContactMessages, StockReceipts, ProductReviews & cột ImageUrl tồn tại
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

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProductVariants')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProductVariants' AND COLUMN_NAME = 'ImageUrl')
        ALTER TABLE [ProductVariants] ADD [ImageUrl] nvarchar(max) NULL;
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Orders')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'CouponCode')
        ALTER TABLE [Orders] ADD [CouponCode] nvarchar(max) NULL;
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'DiscountAmount')
        ALTER TABLE [Orders] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Coupons')
BEGIN
    CREATE TABLE [Coupons] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(30) NOT NULL,
        [Description] nvarchar(200) NOT NULL,
        [DiscountType] nvarchar(max) NOT NULL,
        [DiscountValue] decimal(18,2) NOT NULL,
        [MaxDiscountAmount] decimal(18,2) NULL,
        [MinOrderValue] decimal(18,2) NOT NULL DEFAULT 0,
        [UsageLimit] int NOT NULL DEFAULT 100,
        [UsedCount] int NOT NULL DEFAULT 0,
        [ExpiryDate] datetime2 NOT NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Coupons] PRIMARY KEY ([Id])
    );
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BankSettings')
BEGIN
    CREATE TABLE [BankSettings] (
        [Id] int NOT NULL IDENTITY,
        [BankCode] nvarchar(30) NOT NULL,
        [BankName] nvarchar(150) NOT NULL,
        [AccountNumber] nvarchar(50) NOT NULL,
        [AccountHolder] nvarchar(120) NOT NULL,
        [Branch] nvarchar(150) NULL,
        [QrTemplate] nvarchar(30) NOT NULL DEFAULT 'compact2',
        [TransferPrefix] nvarchar(20) NOT NULL DEFAULT 'DH',
        [Note] nvarchar(300) NULL,
        [IsActive] bit NOT NULL DEFAULT 1,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_BankSettings] PRIMARY KEY ([Id])
    );
END
");
            }
            catch { /* Không chặn luồng khởi động */ }

            // Seed dữ liệu mẫu cho Mã giảm giá (Coupons) và Cấu hình ngân hàng (BankSettings)
            try
            {
                if (!await db.Coupons.AnyAsync())
                {
                    db.Coupons.AddRange(
                        new Coupon
                        {
                            Code = "TIEUDAO50",
                            Description = "Giảm ngay 50.000đ cho mọi đơn hàng từ 500.000đ",
                            DiscountType = "Fixed",
                            DiscountValue = 50000,
                            MinOrderValue = 500000,
                            UsageLimit = 200,
                            UsedCount = 18,
                            ExpiryDate = DateTime.UtcNow.AddMonths(6),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Coupon
                        {
                            Code = "YONEX10",
                            Description = "Giảm 10% (tối đa 300.000đ) cho đơn từ 1.500.000đ",
                            DiscountType = "Percent",
                            DiscountValue = 10,
                            MaxDiscountAmount = 300000,
                            MinOrderValue = 1500000,
                            UsageLimit = 100,
                            UsedCount = 24,
                            ExpiryDate = DateTime.UtcNow.AddMonths(6),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Coupon
                        {
                            Code = "PRO200K",
                            Description = "Giảm 200.000đ khi mua vợt/giày đơn từ 3.000.000đ",
                            DiscountType = "Fixed",
                            DiscountValue = 200000,
                            MinOrderValue = 3000000,
                            UsageLimit = 80,
                            UsedCount = 12,
                            ExpiryDate = DateTime.UtcNow.AddMonths(4),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Coupon
                        {
                            Code = "VIP300K",
                            Description = "Ưu đãi VIP giảm 300.000đ cho đơn từ 5.000.000đ",
                            DiscountType = "Fixed",
                            DiscountValue = 300000,
                            MinOrderValue = 5000000,
                            UsageLimit = 50,
                            UsedCount = 5,
                            ExpiryDate = DateTime.UtcNow.AddMonths(6),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        }
                    );
                    await db.SaveChangesAsync();
                }

                if (!await db.BankSettings.AnyAsync())
                {
                    db.BankSettings.AddRange(
                        new BankSetting
                        {
                            BankCode = "MB",
                            BankName = "MB Bank (Ngân hàng Quân Đội)",
                            AccountNumber = "0900000000",
                            AccountHolder = "TIEU DAO BADMINTON",
                            Branch = "Chi nhánh Quận 7 - TP. Hồ Chí Minh",
                            QrTemplate = "compact2",
                            TransferPrefix = "DH",
                            Note = "Quét mã VietQR để tự động điền chính xác số tiền và nội dung mã đơn hàng.",
                            IsActive = true,
                            UpdatedAt = DateTime.UtcNow
                        },
                        new BankSetting
                        {
                            BankCode = "VCB",
                            BankName = "Vietcombank (Ngân hàng Ngoại Thương)",
                            AccountNumber = "1028889999",
                            AccountHolder = "CONG TY TNHH TIEU DAO SPORT",
                            Branch = "Chi nhánh Nam Sài Gòn",
                            QrTemplate = "compact2",
                            TransferPrefix = "TD",
                            Note = "Tài khoản doanh nghiệp nhận thanh toán đơn hàng sỉ & lẻ.",
                            IsActive = false,
                            UpdatedAt = DateTime.UtcNow
                        }
                    );
                    await db.SaveChangesAsync();
                }
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

            // Tự động kiểm tra và chuẩn hóa toàn bộ dữ liệu Size, Màu sắc & Ảnh theo màu cho database hiện có
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

            // ===== 1) VỢT (20 sản phẩm) =====
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
                        var colorImg = ResolveVariantColorImage(true, false, false, p.Name, racketColors[cIdx], p.Id);
                        db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = racketSizes[sIdx],
                            Color = racketColors[cIdx],
                            ImageUrl = colorImg,
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
                    var colorImg = ResolveVariantColorImage(false, true, false, p.Name, color, p.Id);
                    foreach (var size in shoeSizes)
                    {
                        db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = size,
                            Color = color,
                            ImageUrl = colorImg,
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
                {
                    foreach (var color in clothColors)
                    {
                        var colorImg = ResolveVariantColorImage(false, false, true, p.Name, color, p.Id);
                        db.ProductVariants.Add(new ProductVariant
                        {
                            ProductId = p.Id,
                            Size = size,
                            Color = color,
                            ImageUrl = colorImg,
                            Stock = rnd.Next(5, 20),
                            Sku = $"CLT-{p.Id:D4}-{size}-{color[..1].ToUpper()}"
                        });
                    }
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
                var qty = rnd.Next(2, 10);
                var unit = p.SalePrice ?? p.Price;

                db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductName = p.Name,
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

        /// <summary>
        /// Ánh xạ thông minh từ Màu sắc sản phẩm (Quần áo, Giày, Vợt) sang ảnh preview đúng màu sắc thực tế trong kho ảnh
        /// </summary>
        public static string ResolveVariantColorImage(bool isRacket, bool isShoe, bool isApparel, string productName, string? colorName, int productId)
        {
            var c = (colorName ?? "").Trim().ToLowerInvariant();
            var primaryPart = c.Contains('/') ? c.Split('/')[0].Trim() : c;

            // 1. QUẦN ÁO (Clothes): c1..c12.jfif
            if (isApparel)
            {
                // Nhóm ảnh áo màu Đen thực tế: c7, c9, c11, c12
                var blackClothes = new[] { "/img/products/clothes/c7.jfif", "/img/products/clothes/c9.jfif", "/img/products/clothes/c11.jfif", "/img/products/clothes/c12.jfif" };
                // Nhóm ảnh áo màu Trắng thực tế: c1, c2, c4, c6, c8, c10
                var whiteClothes = new[] { "/img/products/clothes/c1.jfif", "/img/products/clothes/c2.jfif", "/img/products/clothes/c4.jfif", "/img/products/clothes/c6.jfif", "/img/products/clothes/c8.jfif", "/img/products/clothes/c10.jfif" };
                // Nhóm ảnh áo màu Xanh Navy / Xanh Dương thực tế: c5, c3
                var navyClothes = new[] { "/img/products/clothes/c5.jfif", "/img/products/clothes/c3.jfif" };

                if (primaryPart.Contains("đen") || primaryPart.Contains("black"))
                    return blackClothes[Math.Abs(productId) % blackClothes.Length];
                if (primaryPart.Contains("trắng") || primaryPart.Contains("white"))
                    return whiteClothes[Math.Abs(productId) % whiteClothes.Length];
                if (primaryPart.Contains("xanh") || primaryPart.Contains("navy") || primaryPart.Contains("blue"))
                    return navyClothes[Math.Abs(productId) % navyClothes.Length];

                return whiteClothes[Math.Abs(productId) % whiteClothes.Length];
            }

            // 2. GIÀY CẦU LÔNG (Shoes): s1..s12.jfif
            if (isShoe)
            {
                if (primaryPart.Contains("trắng") || primaryPart.Contains("white"))
                {
                    if (c.Contains("đỏ") || c.Contains("cam") || c.Contains("tím") || c.Contains("hồng"))
                        return "/img/products/shoes/s7.jfif"; // Giày Trắng phối Đỏ/Hồng
                    if (c.Contains("vàng") || c.Contains("ngọc"))
                        return (productId % 2 == 0) ? "/img/products/shoes/s12.jfif" : "/img/products/shoes/s5.jfif"; // Giày Trắng phối Vàng Gold / Kem Ngọc
                    if (c.Contains("đen"))
                        return "/img/products/shoes/s8.jfif"; // Giày Trắng phối Đen
                    // Trắng phối Xanh
                    return (productId % 2 == 0) ? "/img/products/shoes/s2.jfif" : "/img/products/shoes/s6.jfif";
                }

                if (primaryPart.Contains("đen") || primaryPart.Contains("black"))
                {
                    return (productId % 2 == 0) ? "/img/products/shoes/s11.jfif" : "/img/products/shoes/s9.jfif"; // Giày Đen chủ đạo
                }

                if (primaryPart.Contains("đỏ") || primaryPart.Contains("red"))
                {
                    var redShoes = new[] { "/img/products/shoes/s3.jfif", "/img/products/shoes/s10.jfif", "/img/products/shoes/s1.jfif" };
                    return redShoes[Math.Abs(productId) % redShoes.Length]; // Giày Đỏ chủ đạo
                }

                if (primaryPart.Contains("xanh") || primaryPart.Contains("navy") || primaryPart.Contains("cyan"))
                {
                    return (c.Contains("cyan") || c.Contains("trắng"))
                        ? "/img/products/shoes/s2.jfif"
                        : "/img/products/shoes/s4.jfif"; // Giày Xanh chủ đạo
                }

                if (primaryPart.Contains("xám") || primaryPart.Contains("bạc"))
                {
                    return "/img/products/shoes/s6.jfif";
                }

                return "/img/products/shoes/s2.jfif";
            }

            // 3. VỢT CẦU LÔNG (Rackets): r1..r12.jfif
            if (isRacket)
            {
                if (primaryPart.Contains("trắng") || primaryPart.Contains("bạch"))
                {
                    if (c.Contains("xanh")) return "/img/products/rackets/r11.jfif";
                    if (c.Contains("tím") || c.Contains("hồng")) return "/img/products/rackets/r6.jfif";
                    return (productId % 2 == 0) ? "/img/products/rackets/r4.jfif" : "/img/products/rackets/r1.jfif";
                }

                if (primaryPart.Contains("vàng") || c.Contains("vàng lightning") || c.Contains("vàng rồng") || c.Contains("tia chớp"))
                {
                    return c.Contains("đen") ? "/img/products/rackets/r8.jfif" : "/img/products/rackets/r3.jfif";
                }

                if (primaryPart.Contains("đỏ") || c.Contains("đỏ cherry") || c.Contains("đỏ ruby") || c.Contains("hổ phách") || c.Contains("sunburst") || c.Contains("bão táp") || c.Contains("đỏ shiny"))
                {
                    if (productName.Contains("Astrox 99", StringComparison.OrdinalIgnoreCase))
                        return "/img/products/rackets/r1.jfif";
                    return (productId % 2 == 0) ? "/img/products/rackets/r12.jfif" : "/img/products/rackets/r9.jfif";
                }

                if (primaryPart.Contains("hồng") || primaryPart.Contains("tím"))
                {
                    return "/img/products/rackets/r6.jfif";
                }

                if (primaryPart.Contains("xanh"))
                {
                    if (c.Contains("chuối") || c.Contains("neon") || c.Contains("lục") || c.Contains("rêu"))
                        return "/img/products/rackets/r7.jfif";
                    return (productId % 2 == 0) ? "/img/products/rackets/r5.jfif" : "/img/products/rackets/r2.jfif";
                }

                if (primaryPart.Contains("đen") || primaryPart.Contains("xám") || primaryPart.Contains("bạc"))
                {
                    if (c.Contains("đỏ")) return "/img/products/rackets/r10.jfif";
                    if (c.Contains("xanh")) return "/img/products/rackets/r7.jfif";
                    if (c.Contains("vàng") || c.Contains("cam")) return "/img/products/rackets/r8.jfif";
                    return (productId % 2 == 0) ? "/img/products/rackets/r10.jfif" : "/img/products/rackets/r2.jfif";
                }

                return "/img/products/rackets/r1.jfif";
            }

            return "/img/demo/placeholder.jpg";
        }

        private static async Task NormalizeProductVariantsAsync(ApplicationDbContext db)
        {
            if (!await db.Products.AnyAsync()) return;

            var products = await db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Images)
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
                                var colorImg = ResolveVariantColorImage(true, false, false, p.Name, color, p.Id);
                                var existing = variantIndex < existingVariants.Count ? existingVariants[variantIndex] : null;

                                if (existing != null)
                                {
                                    existing.Size = size;
                                    existing.Color = color;
                                    existing.ImageUrl = colorImg;
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
                                        ImageUrl = colorImg,
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

                    foreach (var color in targetColors)
                    {
                        var colorImg = ResolveVariantColorImage(false, true, false, p.Name, color, p.Id);
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
                                    ImageUrl = colorImg,
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

                    foreach (var color in distinctColors)
                    {
                        var colorImg = ResolveVariantColorImage(false, false, true, p.Name, color, p.Id);
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
                                ImageUrl = colorImg,
                                Stock = rnd.Next(6, 18),
                                Sku = $"CLT-{p.Id:D4}-XXL-{color[..1].ToUpper()}"
                            });
                            hasChanges = true;
                        }
                    }
                }

                // Đồng bộ ImageUrl theo màu cho tất cả các biến thể hiện có nếu chưa có ImageUrl
                foreach (var v in p.Variants)
                {
                    if (string.IsNullOrWhiteSpace(v.ImageUrl) && !string.IsNullOrWhiteSpace(v.Color))
                    {
                        v.ImageUrl = ResolveVariantColorImage(isRacket, isShoe, isApparel, p.Name, v.Color, p.Id);
                        hasChanges = true;
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
