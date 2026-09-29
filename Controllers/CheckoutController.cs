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

        public CheckoutController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_KEY) ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            var total = cart.Sum(x => x.UnitPrice * x.Quantity);
            var shipping = total >= 499000 ? 0 : 30000;

            var vm = new CheckoutVM
            {
                Items = cart,
                TotalAmount = total,
                ShippingFee = shipping,
                PaymentMethod = "COD"
            };

            // Nếu khách đã đăng nhập, tự điền tên/email
            if (User?.Identity?.IsAuthenticated == true)
            {
                vm.FullName = User.Identity.Name ?? "";
            }

            return View(vm);
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
            var grandTotal = total + shipping;

            if (!ModelState.IsValid)
            {
                vm.Items = cart;
                vm.TotalAmount = total;
                vm.ShippingFee = shipping;
                return View(vm);
            }

            // 1. Tạo đơn hàng mới
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? (User?.Identity?.IsAuthenticated == true ? User.Identity.Name! : "guest");

            var order = new Order
            {
                UserId = userId,
                FullName = vm.FullName.Trim(),
                Phone = vm.Phone.Trim(),
                Address = string.IsNullOrWhiteSpace(vm.Note) 
                    ? vm.Address.Trim() 
                    : $"{vm.Address.Trim()} (Ghi chú: {vm.Note.Trim()})",
                Total = grandTotal,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _db.Orders.Add(order);
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

            // 3. Xóa sạch giỏ hàng trong Session
            HttpContext.Session.Remove(CART_KEY);

            // 4. Chuyển hướng tới trang đặt hàng thành công
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
            return View(order);
        }
    }
}
