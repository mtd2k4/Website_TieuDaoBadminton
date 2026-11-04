using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.ViewModels
{
    public class CourseRegistrationVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên học viên")]
        [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại liên hệ")]
        [RegularExpression(@"^(0|\+84)[0-9]{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn gói khóa học")]
        public string PackageCode { get; set; } = "Basic"; // "Basic" | "Advanced"

        [Required(ErrorMessage = "Vui lòng chọn độ tuổi học viên")]
        public string AgeGroup { get; set; } = "Người đi làm / Phong trào (Trên 22 tuổi)";

        [Required(ErrorMessage = "Vui lòng chọn lịch học mong muốn")]
        public string Schedule { get; set; } = "T2 - T4 - T6 Tối (18:30 - 20:30)";

        [Required(ErrorMessage = "Vui lòng chọn cơ sở sân tập")]
        public string Facility { get; set; } = "Cơ sở Quận 7 - Sân Tiêu Đao Arena (123 Đường Cầu Lông)";

        [StringLength(500, ErrorMessage = "Ghi chú không vượt quá 500 ký tự")]
        public string? Note { get; set; }
    }
}

