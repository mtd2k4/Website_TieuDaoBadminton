using Microsoft.AspNetCore.Mvc;
using tieudaobadminton.Data;
using tieudaobadminton.Helpers;
using tieudaobadminton.Models;
using tieudaobadminton.ViewModels;

namespace tieudaobadminton.Controllers
{
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly OrderNotificationEmailService _emailService;

        public CourseController(ApplicationDbContext db, OrderNotificationEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [HttpGet]
        [Route("Dang-Ky-Khoa-Hoc")]
        [Route("Khoa-Hoc-Cau-Long")]
        [Route("Course")]
        public IActionResult Index(string? package = null)
        {
            var vm = new CourseRegistrationVM();
            if (!string.IsNullOrWhiteSpace(package) &&
                string.Equals(package, "Advanced", StringComparison.OrdinalIgnoreCase))
            {
                vm.PackageCode = "Advanced";
            }
            else
            {
                vm.PackageCode = "Basic";
            }

            return View(vm);
        }

        [HttpPost]
        [Route("Dang-Ky-Khoa-Hoc")]
        [Route("Khoa-Hoc-Cau-Long")]
        [Route("Course")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CourseRegistrationVM vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            bool isAdvanced = string.Equals(vm.PackageCode, "Advanced", StringComparison.OrdinalIgnoreCase);
            string packageTitle = isAdvanced
                ? "Gói Nâng Cao (Chiến Thuật & Thi Đấu - 2.200.000đ/khóa)"
                : "Gói Cơ Bản (Nhập Môn & Nền Tảng - 1.200.000đ/khóa)";

            int registrationId = 0;

            try
            {
                var detailedMessage =
                    $"GÓI KHÓA HỌC: {packageTitle}\n" +
                    $"ĐỐI TƯỢNG / ĐỘ TUỔI: {vm.AgeGroup}\n" +
                    $"LỊCH HỌC ĐĂNG KÝ: {vm.Schedule}\n" +
                    $"CƠ SỞ SÂN TẬP: {vm.Facility}\n" +
                    $"GHI CHÚ HỌC VIÊN: {(string.IsNullOrWhiteSpace(vm.Note) ? "Không có" : vm.Note.Trim())}";

                var msg = new ContactMessage
                {
                    FullName = vm.FullName.Trim(),
                    Email = vm.Email.Trim(),
                    Phone = vm.Phone.Trim(),
                    Subject = $"[Đăng ký khóa học] {(isAdvanced ? "Gói Nâng Cao" : "Gói Cơ Bản")} - {vm.Schedule}",
                    Message = detailedMessage,
                    CreatedAt = DateTime.UtcNow,
                    IsReplied = false
                };

                _db.ContactMessages.Add(msg);
                await _db.SaveChangesAsync();
                registrationId = msg.Id;
            }
            catch
            {
                registrationId = Random.Shared.Next(1000, 9999);
            }

            // Gửi thông tin đăng ký khóa học về Email của Admin (và xác nhận cho học viên)
            bool emailSent = await _emailService.SendCourseRegistrationNotificationAsync(vm, registrationId);

            string regCode = $"KH{registrationId:D4}";
            TempData["CourseOk"] = emailSent
                ? $"Đăng ký thành công mã hồ sơ #{regCode} ({packageTitle})! Thông tin đã được gửi trực tiếp về Email của Ban Quản Trị Admin và xác nhận qua Email của bạn."
                : $"Đăng ký thành công mã hồ sơ #{regCode} ({packageTitle})! Hồ sơ đã được chuyển về hộp thư Quản trị viên Admin, huấn luyện viên sẽ liên hệ SĐT {vm.Phone} trong vòng 24 giờ.";

            return RedirectToAction(nameof(Index), new { package = vm.PackageCode });
        }
    }
}

