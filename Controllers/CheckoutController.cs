using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Helpers;
using tieudaobadminton.Models;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Controllers
{
    public class CheckoutController : Controller
    {
        private const string CART_KEY = "CART";
        private readonly ApplicationDbContext _db;
        private readonly OrderNotificationEmailService _emailService;

        public CheckoutController(ApplicationDbContext db, OrderNotificationEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var total = cart.Sum(x => x.UnitPrice * x.Quantity);
            var shipping = total >= 499000 ? 0 : 30000;

            var availableCoupons = await GetActiveCouponsAsync();
            var bankSetting = await GetActiveBankSettingAsync();

            var vm = new CheckoutVM
            {
                Items = cart,
                TotalAmount = total,
                ShippingFee = shipping,
                PaymentMethod = "COD",
                AvailableCoupons = availableCoupons,
                BankSetting = bankSetting
            };

            // Nếu khách đã đăng nhập, tự điền tên/email
            if (User?.Identity?.IsAuthenticated == true)
            {
                var identityName = User.Identity.Name ?? "";
                vm.FullName = identityName;
                var emailClaim = User.FindFirstValue(ClaimTypes.Email) ?? (identityName.Contains("@") ? identityName : "");
                vm.Email = emailClaim;
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> CheckCoupon(string? code)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var subtotal = cart.Sum(x => x.UnitPrice * x.Quantity);
            var shipping = subtotal >= 499000 ? 0 : 30000;

            if (string.IsNullOrWhiteSpace(code))
            {
                return Json(new
                {
                    valid = false,
                    discountAmount = 0m,
                    grandTotal = subtotal + shipping,
                    message = "Vui lòng nhập mã giảm giá."
                });
            }

            var (coupon, discount, errorMsg) = await EvaluateCouponAsync(code, subtotal);
            if (coupon == null)
            {
                return Json(new
                {
                    valid = false,
                    discountAmount = 0m,
                    grandTotal = subtotal + shipping,
                    message = errorMsg ?? "Mã giảm giá không hợp lệ."
                });
            }

            var grandTotal = Math.Max(0, subtotal + shipping - discount);
            return Json(new
            {
                valid = true,
                code = coupon.Code,
                description = coupon.Description,
                discountAmount = discount,
                grandTotal = grandTotal,
                message = $"Áp dụng mã {coupon.Code} thành công! Giảm {discount:n0} đ."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutVM vm)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var total = cart.Sum(x => x.UnitPrice * x.Quantity);
            var shipping = total >= 499000 ? 0 : 30000;

            Coupon? appliedCoupon = null;
            decimal discountAmount = 0m;

            if (!string.IsNullOrWhiteSpace(vm.CouponCode))
            {
                var (coupon, discount, errorMsg) = await EvaluateCouponAsync(vm.CouponCode, total);
                if (coupon == null)
                {
                    ModelState.AddModelError(nameof(vm.CouponCode), errorMsg ?? "Mã giảm giá không hợp lệ.");
                }
                else
                {
                    appliedCoupon = coupon;
                    discountAmount = discount;
                    vm.CouponCode = coupon.Code;
                }
            }

            var grandTotal = Math.Max(0, total + shipping - discountAmount);

            if (!ModelState.IsValid)
            {
                vm.Items = cart;
                vm.TotalAmount = total;
                vm.ShippingFee = shipping;
                vm.DiscountAmount = discountAmount;
                vm.AvailableCoupons = await GetActiveCouponsAsync();
                vm.BankSetting = await GetActiveBankSettingAsync();
                return View(vm);
            }

            // 1. Tạo đơn hàng mới
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? (User?.Identity?.IsAuthenticated == true ? User.Identity.Name! : "guest");
            var emailTrimmed = vm.Email?.Trim() ?? "";
            var extraInfo = new List<string>();
            if (!string.IsNullOrWhiteSpace(emailTrimmed))
                extraInfo.Add($"Email: {emailTrimmed}");
            if (appliedCoupon != null && discountAmount > 0)
                extraInfo.Add($"Voucher: {appliedCoupon.Code} (-{discountAmount:n0}đ)");
            if (!string.IsNullOrWhiteSpace(vm.Note))
                extraInfo.Add($"Ghi chú: {vm.Note.Trim()}");

            var order = new Order
            {
                UserId = userId,
                FullName = vm.FullName.Trim(),
                Phone = vm.Phone.Trim(),
                Address = extraInfo.Count > 0
                    ? $"{vm.Address.Trim()} ({string.Join(" | ", extraInfo)})"
                    : vm.Address.Trim(),
                CouponCode = appliedCoupon?.Code,
                DiscountAmount = discountAmount,
                Total = grandTotal,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);

            // Tăng số lượt sử dụng mã giảm giá
            if (appliedCoupon != null)
            {
                appliedCoupon.UsedCount += 1;
            }

            await _db.SaveChangesAsync();

            // 2. Thêm từng mục OrderItem và trừ tồn kho Variant
            foreach (var item in cart)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    VariantId = item.VariantId,
                    ProductName = item.ProductName,
                    Size = item.Size,
                    Color = item.Color,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                };
                _db.OrderItems.Add(orderItem);

                // Trừ tồn kho nếu có variant
                if (item.VariantId.HasValue)
                {
                    var variant = await _db.ProductVariants.FindAsync(item.VariantId.Value);
                    if (variant != null)
                    {
                        variant.Stock = Math.Max(0, variant.Stock - item.Quantity);
                    }
                }
            }

            await _db.SaveChangesAsync();

            // 3. Gửi thông báo Email SMTP về cho Khách hàng (User) và Quản trị viên (Admin)
            var emailSent = await _emailService.SendOrderNotificationsAsync(
                order,
                cart,
                emailTrimmed,
                vm.PaymentMethod,
                shipping
            );

            TempData["CustomerEmail"] = emailTrimmed;
            TempData["EmailSent"] = emailSent;

            // 4. Xóa sạch giỏ hàng trong Session
            HttpContext.Session.Remove(CART_KEY);

            // 5. Chuyển hướng tới trang đặt hàng thành công
            return RedirectToAction(nameof(Success), new { id = order.Id, method = vm.PaymentMethod });
        }

        [HttpGet]
        public async Task<IActionResult> Success(int id, string? method)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.PaymentMethod = method ?? "COD";
            ViewBag.BankSetting = await GetActiveBankSettingAsync();
            return View(order);
        }

        private async Task<List<Coupon>> GetActiveCouponsAsync()
        {
            var now = DateTime.UtcNow;
            return await _db.Coupons
                .Where(c => c.IsActive && c.ExpiryDate > now && c.UsedCount < c.UsageLimit)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();
        }

        private async Task<BankSetting> GetActiveBankSettingAsync()
        {
            var bank = await _db.BankSettings
                .Where(b => b.IsActive)
                .OrderByDescending(b => b.UpdatedAt)
                .FirstOrDefaultAsync();

            if (bank != null) return bank;

            var anyBank = await _db.BankSettings.FirstOrDefaultAsync();
            if (anyBank != null) return anyBank;

            return new BankSetting
            {
                BankCode = "MB",
                BankName = "MB Bank (Ngân hàng Quân Đội)",
                AccountNumber = "0900000000",
                AccountHolder = "TIEU DAO BADMINTON",
                Branch = "Chi nhánh Quận 7 - TP. Hồ Chí Minh",
                QrTemplate = "compact2",
                TransferPrefix = "DH",
                Note = "Quét mã VietQR để tự động điền chính xác số tiền và nội dung mã đơn hàng.",
                IsActive = true
            };
        }

        private async Task<(Coupon? coupon, decimal discountAmount, string? errorMessage)> EvaluateCouponAsync(string rawCode, decimal subtotal)
        {
            var normalized = rawCode.Trim().ToUpperInvariant();
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code.ToUpper() == normalized);

            if (coupon == null || !coupon.IsActive)
            {
                return (null, 0m, "Mã giảm giá không tồn tại hoặc đã ngừng kích hoạt.");
            }

            if (coupon.ExpiryDate <= DateTime.UtcNow)
            {
                return (null, 0m, $"Mã giảm giá {coupon.Code} đã hết hạn sử dụng.");
            }

            if (coupon.UsedCount >= coupon.UsageLimit)
            {
                return (null, 0m, $"Mã giảm giá {coupon.Code} đã hết lượt sử dụng.");
            }

            if (subtotal < coupon.MinOrderValue)
            {
                return (null, 0m, $"Đơn hàng tối thiểu từ {coupon.MinOrderValue:n0} đ mới áp dụng được mã {coupon.Code}.");
            }

            decimal discount = 0m;
            if (string.Equals(coupon.DiscountType, "Percent", StringComparison.OrdinalIgnoreCase))
            {
                discount = Math.Round(subtotal * (coupon.DiscountValue / 100m), 0);
                if (coupon.MaxDiscountAmount.HasValue && coupon.MaxDiscountAmount.Value > 0 && discount > coupon.MaxDiscountAmount.Value)
                {
                    discount = coupon.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discount = coupon.DiscountValue;
            }

            if (discount > subtotal)
            {
                discount = subtotal;
            }

            return (coupon, discount, null);
        }
    }
}
