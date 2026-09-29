using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.ViewModels
{
    public class ContactVM
    {
        [Required(ErrorMessage = "Nhập họ tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Nhập email"), EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string Phone { get; set; } = "";

        public string? Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung liên hệ")]
        public string Message { get; set; } = "";
    }
}
