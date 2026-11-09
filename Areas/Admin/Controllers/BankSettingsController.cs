using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class BankSettingsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BankSettingsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? editId)
        {
            var list = await _db.BankSettings
                .OrderByDescending(b => b.IsActive)
                .ThenByDescending(b => b.UpdatedAt)
                .ToListAsync();

            BankSetting formModel;
            if (editId.HasValue)
            {
                formModel = list.FirstOrDefault(x => x.Id == editId.Value) ?? new BankSetting();
            }
            else
            {
                formModel = list.FirstOrDefault(x => x.IsActive) ?? list.FirstOrDefault() ?? new BankSetting();
            }

            ViewBag.BankList = list;
            ViewBag.IsCreatingNew = editId == 0;
            if (editId == 0)
            {
                formModel = new BankSetting
                {
                    BankCode = "VCB",
                    BankName = "Vietcombank (Ngân hàng Ngoại Thương)",
                    AccountNumber = "",
                    AccountHolder = "TIEU DAO BADMINTON",
                    Branch = "Chi nhánh TP. Hồ Chí Minh",
                    QrTemplate = "compact2",
                    TransferPrefix = "DH",
                    Note = "Quét mã VietQR để tự động điền chính xác số tiền và nội dung mã đơn hàng.",
                    IsActive = true
                };
            }

            return View(formModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(BankSetting model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BankList = await _db.BankSettings
                    .OrderByDescending(b => b.IsActive)
                    .ThenByDescending(b => b.UpdatedAt)
                    .ToListAsync();
                return View("Index", model);
            }

            model.BankCode = model.BankCode.Trim().ToUpperInvariant();
            model.BankName = model.BankName.Trim();
            model.AccountNumber = model.AccountNumber.Trim();
            model.AccountHolder = model.AccountHolder.Trim().ToUpperInvariant();
            model.Branch = model.Branch?.Trim();
            model.QrTemplate = string.IsNullOrWhiteSpace(model.QrTemplate) ? "compact2" : model.QrTemplate.Trim();
            model.TransferPrefix = string.IsNullOrWhiteSpace(model.TransferPrefix) ? "DH" : model.TransferPrefix.Trim().ToUpperInvariant();
            model.Note = model.Note?.Trim();
            model.UpdatedAt = DateTime.UtcNow;

            if (model.IsActive)
            {
                var others = await _db.BankSettings.Where(b => b.Id != model.Id && b.IsActive).ToListAsync();
                foreach (var item in others)
                {
                    item.IsActive = false;
                }
            }

            if (model.Id == 0)
            {
                _db.BankSettings.Add(model);
                await _db.SaveChangesAsync();
                TempData["Ok"] = $"Đã thêm cấu hình ngân hàng {model.BankName} ({model.AccountNumber}) thành công!";
            }
            else
            {
                var existing = await _db.BankSettings.FindAsync(model.Id);
                if (existing == null)
                {
                    return NotFound();
                }

                existing.BankCode = model.BankCode;
                existing.BankName = model.BankName;
                existing.AccountNumber = model.AccountNumber;
                existing.AccountHolder = model.AccountHolder;
                existing.Branch = model.Branch;
                existing.QrTemplate = model.QrTemplate;
                existing.TransferPrefix = model.TransferPrefix;
                existing.Note = model.Note;
                existing.IsActive = model.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                TempData["Ok"] = $"Đã cập nhật cấu hình ngân hàng {existing.BankName} ({existing.AccountNumber}) thành công!";
            }

            // Đảm bảo luôn có ít nhất 1 ngân hàng Active
            if (!await _db.BankSettings.AnyAsync(b => b.IsActive))
            {
                var first = await _db.BankSettings.FirstOrDefaultAsync();
                if (first != null)
                {
                    first.IsActive = true;
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id)
        {
            var all = await _db.BankSettings.ToListAsync();
            var target = all.FirstOrDefault(x => x.Id == id);
            if (target == null) return NotFound();

            foreach (var b in all)
            {
                b.IsActive = (b.Id == id);
            }
            target.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            TempData["Ok"] = $"Đã kích hoạt tài khoản {target.BankName} - {target.AccountNumber} làm cổng thanh toán VietQR mặc định!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var target = await _db.BankSettings.FindAsync(id);
            if (target == null) return NotFound();

            var count = await _db.BankSettings.CountAsync();
            if (count <= 1)
            {
                TempData["Err"] = "Hệ thống cần giữ lại ít nhất 1 tài khoản ngân hàng để hiển thị VietQR thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            bool wasActive = target.IsActive;
            _db.BankSettings.Remove(target);
            await _db.SaveChangesAsync();

            if (wasActive)
            {
                var next = await _db.BankSettings.OrderByDescending(b => b.UpdatedAt).FirstOrDefaultAsync();
                if (next != null)
                {
                    next.IsActive = true;
                    await _db.SaveChangesAsync();
                }
            }

            TempData["Ok"] = $"Đã xóa tài khoản ngân hàng {target.BankName} ({target.AccountNumber}).";
            return RedirectToAction(nameof(Index));
        }
    }
}

