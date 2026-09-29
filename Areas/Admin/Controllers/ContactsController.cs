using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Data;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ContactsController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Contacts
        public async Task<IActionResult> Index(string? status)
        {
            var q = _db.ContactMessages.AsQueryable();

            if (status == "unread")
            {
                q = q.Where(c => !c.IsReplied);
            }
            else if (status == "replied")
            {
                q = q.Where(c => c.IsReplied);
            }

            ViewBag.CurrentStatus = status ?? "";
            ViewBag.TotalCount = await _db.ContactMessages.CountAsync();
            ViewBag.UnrepliedCount = await _db.ContactMessages.CountAsync(c => !c.IsReplied);
            ViewBag.RepliedCount = await _db.ContactMessages.CountAsync(c => c.IsReplied);

            var messages = await q.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(messages);
        }

        // GET: /Admin/Contacts/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            return View(msg);
        }

        // POST: /Admin/Contacts/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? staffNote)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            msg.IsReplied = !msg.IsReplied;
            if (staffNote != null)
            {
                msg.StaffNote = staffNote.Trim();
            }

            await _db.SaveChangesAsync();

            TempData["Ok"] = msg.IsReplied
                ? "Đã đánh dấu tin nhắn là 'Đã phản hồi'."
                : "Đã chuyển tin nhắn về trạng thái 'Chưa xử lý'.";

            return RedirectToAction(nameof(Detail), new { id = msg.Id });
        }

        // POST: /Admin/Contacts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _db.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            _db.ContactMessages.Remove(msg);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Đã xóa tin nhắn liên hệ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
