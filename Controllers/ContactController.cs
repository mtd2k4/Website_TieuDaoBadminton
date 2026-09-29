using Microsoft.AspNetCore.Mvc;
using tieudaobadminton.Data;
using tieudaobadminton.Models;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ContactController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        [Route("Lien-He")]
        [Route("Contact")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Route("Lien-He")]
        [Route("Contact")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var msg = new ContactMessage
                {
                    FullName = vm.FullName.Trim(),
                    Email = vm.Email.Trim(),
                    Phone = vm.Phone.Trim(),
                    Subject = vm.Subject?.Trim(),
                    Message = vm.Message.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsReplied = false
                };

                _db.ContactMessages.Add(msg);
                await _db.SaveChangesAsync();

                TempData["Ok"] = "Cảm ơn bạn! Yêu cầu tư vấn / liên hệ của bạn đã được gửi thành công. Shop sẽ liên hệ phản hồi trong thời gian sớm nhất.";
            }
            catch
            {
                TempData["Ok"] = "Cảm ơn bạn! Yêu cầu của bạn đã được ghi nhận. Shop sẽ liên hệ hỗ trợ bạn sớm.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
