using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Controllers
{
    [Route("Tra-Cuu-Don-Hang")]
    public class OrderTrackingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrderTrackingController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return View(new List<Order>());
            }

            var clean = keyword.Trim().Replace("#", "").Replace("DH", "", StringComparison.OrdinalIgnoreCase).Trim();
            bool isNumber = int.TryParse(clean, out int orderId);

            IQueryable<Order> query = _db.Orders
                .Include(o => o.Items)
                .AsNoTracking();

            List<Order> orders;

            if (isNumber && clean.Length <= 6)
            {
                // Tìm theo mã đơn hàng trước
                orders = await query
                    .Where(o => o.Id == orderId)
                    .ToListAsync();

                // Nếu không thấy mã đơn, tìm tiếp theo số điện thoại (phòng khi số điện thoại ngắn)
                if (orders.Count == 0)
                {
                    orders = await query
                        .Where(o => o.Phone.Contains(clean))
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(10)
                        .ToListAsync();
                }
            }
            else
            {
                // Tìm theo số điện thoại
                orders = await query
                    .Where(o => o.Phone.Contains(clean) || o.Phone.Contains(keyword.Trim()))
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync();
            }

            ViewBag.Keyword = keyword;
            ViewBag.HasSearched = true;

            return View(orders);
        }
    }
}
