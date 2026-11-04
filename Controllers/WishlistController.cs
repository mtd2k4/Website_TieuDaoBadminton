using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Helpers;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    public class WishlistController : Controller
    {
        public const string WISHLIST_KEY = "WISHLIST";
        private readonly ApplicationDbContext _db;

        public WishlistController(ApplicationDbContext db)
        {
            _db = db;
        }

        [Route("Yeu-Thich")]
        [Route("Wishlist")]
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var ids = HttpContext.Session.GetObject<List<int>>(WISHLIST_KEY) ?? new List<int>();

            var products = await _db.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();

            // Sắp xếp theo thứ tự thêm vào danh sách yêu thích
            var orderedProducts = products
                .OrderBy(p => ids.IndexOf(p.Id))
                .ToList();

            return View(orderedProducts);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            var ids = HttpContext.Session.GetObject<List<int>>(WISHLIST_KEY) ?? new List<int>();
            bool isAdded;

            if (ids.Contains(productId))
            {
                ids.Remove(productId);
                isAdded = false;
            }
            else
            {
                ids.Add(productId);
                isAdded = true;
            }

            HttpContext.Session.SetObject(WISHLIST_KEY, ids);

            return Json(new
            {
                success = true,
                isAdded,
                count = ids.Count,
                message = isAdded
                    ? $"Đã lưu \"{product.Name}\" vào danh sách yêu thích."
                    : $"Đã bỏ lưu \"{product.Name}\" khỏi danh sách yêu thích."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId, string? returnUrl = null)
        {
            var ids = HttpContext.Session.GetObject<List<int>>(WISHLIST_KEY) ?? new List<int>();
            ids.Remove(productId);
            HttpContext.Session.SetObject(WISHLIST_KEY, ids);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(WISHLIST_KEY);
            return RedirectToAction(nameof(Index));
        }
    }
}
