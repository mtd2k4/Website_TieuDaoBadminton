using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _db;

        public OrdersController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            var q = _db.Orders.Include(o => o.Items).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                q = q.Where(o => o.FullName.Contains(search) || o.Phone.Contains(search) || o.Id.ToString() == search);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                q = q.Where(o => o.Status == status);
            }

            ViewBag.CurrentStatus = status ?? "";
            ViewBag.Search = search ?? "";

            // Thống kê nhanh
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.PendingOrders = await _db.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.DoneOrders = await _db.Orders.CountAsync(o => o.Status == "Done");
            ViewBag.TotalRevenue = await _db.Orders.Where(o => o.Status == "Done").SumAsync(o => o.Total);

            var list = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(list);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        public async Task<IActionResult> Print(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var allowedStatuses = new[] { "Pending", "Shipping", "Done", "Cancelled" };
            if (!allowedStatuses.Contains(newStatus))
            {
                TempData["Err"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.Status = newStatus;
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã cập nhật đơn hàng #{order.Id:D5} thành công!";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            _db.OrderItems.RemoveRange(order.Items);
            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();

            TempData["Ok"] = $"Đã xóa đơn hàng #{id:D5}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
