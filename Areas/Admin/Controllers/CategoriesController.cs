using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _db.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.Id)
                .ToListAsync();

            return View(categories);
        }

        // GET: /Admin/Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Vui lòng nhập tên danh mục.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = Slugify(model.Name);
            }
            else
            {
                model.Slug = Slugify(model.Slug);
            }

            // Kiểm tra trùng slug
            var exists = await _db.Categories.AnyAsync(c => c.Slug == model.Slug);
            if (exists)
            {
                ModelState.AddModelError("Slug", "Slug danh mục đã tồn tại, vui lòng chọn tên hoặc slug khác.");
                return View(model);
            }

            _db.Categories.Add(model);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã thêm thành công danh mục: {model.Name}";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Categories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        // POST: /Admin/Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id) return BadRequest();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Tên danh mục không được để trống.");
                return View(model);
            }

            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = Slugify(model.Name);
            }
            else
            {
                model.Slug = Slugify(model.Slug);
            }

            // Kiểm tra trùng slug với bản ghi khác
            var exists = await _db.Categories.AnyAsync(c => c.Slug == model.Slug && c.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Slug", "Slug này đã bị trùng với danh mục khác.");
                return View(model);
            }

            cat.Name = model.Name.Trim();
            cat.Slug = model.Slug.Trim();

            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã cập nhật danh mục: {cat.Name}";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null) return NotFound();

            if (cat.Products.Any())
            {
                TempData["Error"] = $"Không thể xóa danh mục '{cat.Name}' vì đang có {cat.Products.Count} sản phẩm liên kết. Vui lòng chuyển sản phẩm sang danh mục khác trước.";
                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã xóa danh mục: {cat.Name}";
            return RedirectToAction(nameof(Index));
        }

        private static string Slugify(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.ToLowerInvariant().Trim();

            string[] vietnameseSigns = new string[]
            {
                "a", "á|à|ả|ã|ạ|ă|ắ|ằ|ẳ|ẵ|ặ|â|ấ|ầ|ẩ|ẫ|ậ",
                "d", "đ",
                "e", "é|è|ẻ|ẽ|ẹ|ê|ế|ề|ể|ễ|ệ",
                "i", "í|ì|ỉ|ĩ|ị",
                "o", "ó|ò|ỏ|õ|ọ|ô|ố|ồ|ổ|ỗ|ộ|ơ|ớ|ờ|ở|ỡ|ợ",
                "u", "ú|ù|ủ|ũ|ụ|ư|ứ|ừ|ử|ữ|ự",
                "y", "ý|ỳ|ỷ|ỹ|ỵ"
            };

            for (int i = 1; i < vietnameseSigns.Length; i += 2)
            {
                text = Regex.Replace(text, vietnameseSigns[i], vietnameseSigns[i - 1]);
            }

            text = Regex.Replace(text, @"[^a-z0-9\s-]", "");
            text = Regex.Replace(text, @"\s+", "-").Trim('-');
            return text;
        }
    }
}
