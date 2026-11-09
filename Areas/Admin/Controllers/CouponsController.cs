using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CouponsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? editId)
        {
            var list = await _db.Coupons
                .OrderByDescending(c => c.IsActive)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            Coupon formModel;
            if (editId.HasValue && editId.Value > 0)
            {
                formModel = list.FirstOrDefault(x => x.Id == editId.Value) ?? new Coupon();
            }
            else
            {
                formModel = new Coupon
                {
                    DiscountType = "Fixed",
                    DiscountValue = 100000,
                    MinOrderValue = 1000000,
                    UsageLimit = 100,
                    ExpiryDate = DateTime.UtcNow.AddMonths(3),
                    IsActive = true
                };
            }

            ViewBag.CouponList = list;
            return View(formModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Coupon model)
        {
            model.Code = (model.Code ?? "").Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(model.Code))
            {
                var duplicate = await _db.Coupons.AnyAsync(c => c.Id != model.Id && c.Code.ToUpper() == model.Code);
                if (duplicate)
                {
                    ModelState.AddModelError(nameof(model.Code), $"Mã giảm giá '{model.Code}' đã tồn tại trong hệ thống.");
                }
            }

            if (model.DiscountType == "Percent" && (model.DiscountValue <= 0 || model.DiscountValue > 100))
            {
                ModelState.AddModelError(nameof(model.DiscountValue), "Mức giảm theo phần trăm (%) phải nằm trong khoảng từ 1 đến 100.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.CouponList = await _db.Coupons
                    .OrderByDescending(c => c.IsActive)
                    .ThenByDescending(c => c.Id)
                    .ToListAsync();
                return View("Index", model);
            }

            if (model.Id == 0)
            {
                model.CreatedAt = DateTime.UtcNow;
                _db.Coupons.Add(model);
                await _db.SaveChangesAsync();
                TempData["Ok"] = $"Đã tạo mã giảm giá {model.Code} thành công!";
            }
            else
            {
                var existing = await _db.Coupons.FindAsync(model.Id);
                if (existing == null) return NotFound();

                existing.Code = model.Code;
                existing.Description = model.Description.Trim();
                existing.DiscountType = model.DiscountType;
                existing.DiscountValue = model.DiscountValue;
                existing.MaxDiscountAmount = model.DiscountType == "Percent" ? model.MaxDiscountAmount : null;
                existing.MinOrderValue = model.MinOrderValue;
                existing.UsageLimit = Math.Max(1, model.UsageLimit);
                existing.ExpiryDate = model.ExpiryDate;
                existing.IsActive = model.IsActive;

                await _db.SaveChangesAsync();
                TempData["Ok"] = $"Đã cập nhật mã giảm giá {existing.Code} thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var coupon = await _db.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            coupon.IsActive = !coupon.IsActive;
            await _db.SaveChangesAsync();

            TempData["Ok"] = coupon.IsActive
                ? $"Đã kích hoạt lại mã giảm giá {coupon.Code}."
                : $"Đã tạm khóa mã giảm giá {coupon.Code}.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _db.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            _db.Coupons.Remove(coupon);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã xóa mã giảm giá {coupon.Code}.";
            return RedirectToAction(nameof(Index));
        }
    }
}

