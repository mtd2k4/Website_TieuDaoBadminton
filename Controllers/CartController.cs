using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Helpers;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    public class CartController : Controller
    {
        private const string CART_KEY = "CART";
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db) => _db = db;

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productId, int? variantId, int quantity = 1, string? stringTension = null, string? returnUrl = null)
        {
            quantity = Math.Max(1, quantity);

            var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);
            if (product == null) return NotFound();

            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId.Value && v.ProductId == productId);
                if (variant == null) return BadRequest("Variant không hợp lệ.");
                if (variant.Stock < quantity) return BadRequest("Không đủ tồn kho.");
            }

            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();

            var itemSize = variant?.Size;
            if (!string.IsNullOrWhiteSpace(stringTension))
            {
                itemSize = string.IsNullOrWhiteSpace(itemSize) || itemSize == "-"
                    ? $"Căng cước: {stringTension}"
                    : $"{itemSize} • Cước: {stringTension}";
            }

            // key theo ProductId + VariantId + Size (để phân biệt các mức căng cước khác nhau)
            var item = cart.FirstOrDefault(x => x.ProductId == productId && x.VariantId == variantId && x.Size == itemSize);
            var price = product.SalePrice ?? product.Price;

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = productId,
                    VariantId = variantId,
                    ProductName = product.Name,
                    ImageUrl = product.MainImageUrl,
                    Size = itemSize,
                    Color = variant?.Color,
                    UnitPrice = price,
                    Quantity = quantity
                });
            }
            else
            {
                // cộng dồn, nhưng check tồn kho nếu có variant
                var newQty = item.Quantity + quantity;
                if (variant != null && variant.Stock < newQty) return BadRequest("Không đủ tồn kho.");
                item.Quantity = newQty;
            }

            HttpContext.Session.SetObject(CART_KEY, cart);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int? variantId, int quantity)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            var item = cart.FirstOrDefault(x => x.ProductId == productId && x.VariantId == variantId);
            if (item == null) return RedirectToAction(nameof(Index));

            if (quantity <= 0)
                cart.Remove(item);
            else
                item.Quantity = quantity;

            HttpContext.Session.SetObject(CART_KEY, cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId, int? variantId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            cart.RemoveAll(x => x.ProductId == productId && x.VariantId == variantId);
            HttpContext.Session.SetObject(CART_KEY, cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction(nameof(Index));
        }
    }
}
