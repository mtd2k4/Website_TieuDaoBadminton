using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BrandsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Brands
        public async Task<IActionResult> Index()
        {
            var brands = await _db.Brands
                .Include(b => b.Products)
                .OrderBy(b => b.Id)
                .ToListAsync();

            return View(brands);
        }

        // GET: /Admin/Brands/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Brands/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Vui lòng nhập tên thương hiệu.");
                return View(model);
            }

            var trimmedName = model.Name.Trim();
            var exists = await _db.Brands.AnyAsync(b => b.Name.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                ModelState.AddModelError("Name", "Thương hiệu này đã tồn tại trong hệ thống.");
                return View(model);
            }

            model.Name = trimmedName;
            _db.Brands.Add(model);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã thêm thương hiệu: {model.Name}";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Brands/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        // POST: /Admin/Brands/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand model)
        {
            if (id != model.Id) return BadRequest();

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ModelState.AddModelError("Name", "Tên thương hiệu không được để trống.");
                return View(model);
            }

            var brand = await _db.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            var trimmedName = model.Name.Trim();
            var exists = await _db.Brands.AnyAsync(b => b.Name.ToLower() == trimmedName.ToLower() && b.Id != id);
            if (exists)
            {
                ModelState.AddModelError("Name", "Tên thương hiệu này đã bị trùng với thương hiệu khác.");
                return View(model);
            }

            brand.Name = trimmedName;
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã cập nhật thương hiệu: {brand.Name}";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Brands/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _db.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (brand == null) return NotFound();

            if (brand.Products.Any())
            {
                TempData["Error"] = $"Không thể xóa thương hiệu '{brand.Name}' vì đang có {brand.Products.Count} sản phẩm liên kết. Vui lòng chuyển sản phẩm sang hãng khác trước.";
                return RedirectToAction(nameof(Index));
            }

            _db.Brands.Remove(brand);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã xóa thương hiệu: {brand.Name}";
            return RedirectToAction(nameof(Index));
        }
    }
}
