using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public NewsController(ApplicationDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var q = _db.NewsPosts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(x => x.Title.Contains(search));

            if (status == "published") q = q.Where(x => x.IsPublished);
            if (status == "draft") q = q.Where(x => !x.IsPublished);

            ViewBag.Search = search ?? "";
            ViewBag.Status = status ?? "";

            return View(await q.OrderByDescending(x => x.PublishedAt).ToListAsync());
        }

        public IActionResult Create() => View(new AdminNewsFormVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminNewsFormVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var post = new NewsPost
            {
                Title = vm.Title,
                Slug = Slugify(vm.Title),
                Summary = vm.Summary,
                Content = vm.Content,
                IsPublished = vm.IsPublished,
                CreatedAt = DateTime.UtcNow,
                PublishedAt = DateTime.UtcNow
            };

            if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
                post.ThumbnailUrl = await SaveFile(vm.ThumbnailFile, "uploads/news");

            if (string.IsNullOrWhiteSpace(post.ThumbnailUrl))
                post.ThumbnailUrl = vm.ThumbnailUrl;

            _db.NewsPosts.Add(post);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var post = await _db.NewsPosts.FindAsync(id);
            if (post == null) return NotFound();

            return View(new AdminNewsFormVM
            {
                Id = post.Id,
                Title = post.Title,
                Summary = post.Summary,
                Content = post.Content,
                ThumbnailUrl = post.ThumbnailUrl,
                IsPublished = post.IsPublished
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminNewsFormVM vm)
        {
            var post = await _db.NewsPosts.FindAsync(vm.Id);
            if (post == null) return NotFound();

            if (!ModelState.IsValid) return View(vm);

            post.Title = vm.Title;
            post.Slug = Slugify(vm.Title);
            post.Summary = vm.Summary;
            post.Content = vm.Content;
            post.IsPublished = vm.IsPublished;

            if (vm.ThumbnailFile != null && vm.ThumbnailFile.Length > 0)
                post.ThumbnailUrl = await SaveFile(vm.ThumbnailFile, "uploads/news");
            else
                post.ThumbnailUrl = vm.ThumbnailUrl;

            if (post.IsPublished) post.PublishedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _db.NewsPosts.FindAsync(id);
            if (post == null) return NotFound();

            _db.NewsPosts.Remove(post);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UploadEditorImage(IFormFile upload)
        {
            try
            {
                if (upload == null || upload.Length == 0)
                {
                    return Json(new { error = new { message = "Vui lòng chọn tệp hình ảnh để tải lên." } });
                }

                var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (!allowed.Contains(ext))
                {
                    return Json(new { error = new { message = "Định dạng ảnh không hợp lệ. Chỉ chấp nhận JPG, JPEG, PNG, WEBP, GIF." } });
                }

                if (upload.Length > 8 * 1024 * 1024)
                {
                    return Json(new { error = new { message = "Dung lượng ảnh vượt quá giới hạn (Tối đa 8MB)." } });
                }

                var url = await SaveFile(upload, "uploads/news");
                return Json(new { url });
            }
            catch (Exception ex)
            {
                return Json(new { error = new { message = "Lỗi khi lưu ảnh máy chủ: " + ex.Message } });
            }
        }

        private async Task<string> SaveFile(IFormFile file, string folder)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext)) ext = ".jpg";

            var name = $"{Guid.NewGuid():N}{ext}";
            var dir = Path.Combine(_env.WebRootPath, folder.Replace("/", Path.DirectorySeparatorChar.ToString()));
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, name);
            using var stream = System.IO.File.Create(path);
            await file.CopyToAsync(stream);

            return "/" + folder.Trim('/') + "/" + name;
        }

        private static string Slugify(string s)
        {
            s = s.Trim().ToLowerInvariant();
            var map = new Dictionary<char, char>
            {
                ['á'] = 'a',
                ['à'] = 'a',
                ['ả'] = 'a',
                ['ã'] = 'a',
                ['ạ'] = 'a',
                ['ă'] = 'a',
                ['ắ'] = 'a',
                ['ằ'] = 'a',
                ['ẳ'] = 'a',
                ['ẵ'] = 'a',
                ['ặ'] = 'a',
                ['â'] = 'a',
                ['ấ'] = 'a',
                ['ầ'] = 'a',
                ['ẩ'] = 'a',
                ['ẫ'] = 'a',
                ['ậ'] = 'a',
                ['đ'] = 'd',
                ['é'] = 'e',
                ['è'] = 'e',
                ['ẻ'] = 'e',
                ['ẽ'] = 'e',
                ['ẹ'] = 'e',
                ['ê'] = 'e',
                ['ế'] = 'e',
                ['ề'] = 'e',
                ['ể'] = 'e',
                ['ễ'] = 'e',
                ['ệ'] = 'e',
                ['í'] = 'i',
                ['ì'] = 'i',
                ['ỉ'] = 'i',
                ['ĩ'] = 'i',
                ['ị'] = 'i',
                ['ó'] = 'o',
                ['ò'] = 'o',
                ['ỏ'] = 'o',
                ['õ'] = 'o',
                ['ọ'] = 'o',
                ['ô'] = 'o',
                ['ố'] = 'o',
                ['ồ'] = 'o',
                ['ổ'] = 'o',
                ['ỗ'] = 'o',
                ['ộ'] = 'o',
                ['ơ'] = 'o',
                ['ớ'] = 'o',
                ['ờ'] = 'o',
                ['ở'] = 'o',
                ['ỡ'] = 'o',
                ['ợ'] = 'o',
                ['ú'] = 'u',
                ['ù'] = 'u',
                ['ủ'] = 'u',
                ['ũ'] = 'u',
                ['ụ'] = 'u',
                ['ư'] = 'u',
                ['ứ'] = 'u',
                ['ừ'] = 'u',
                ['ử'] = 'u',
                ['ữ'] = 'u',
                ['ự'] = 'u',
                ['ý'] = 'y',
                ['ỳ'] = 'y',
                ['ỷ'] = 'y',
                ['ỹ'] = 'y',
                ['ỵ'] = 'y'
            };
            s = new string(s.Select(ch => map.ContainsKey(ch) ? map[ch] : ch).ToArray());
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\s-]", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
            return s.Replace(" ", "-");
        }
    }
}
