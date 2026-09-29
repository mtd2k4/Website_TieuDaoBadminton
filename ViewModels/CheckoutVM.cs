using System.ComponentModel.DataAnnotations;
using tieudaobadminton.Models;

namespace tieudaobadminton.ViewModels
{
    public class CheckoutVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (10 chữ số, bắt đầu bằng 0)")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string Address { get; set; } = "";

        [Display(Name = "Ghi chú đơn hàng")]
        public string? Note { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD"; // COD hoặc BankTransfer

        public List<CartItem> Items { get; set; } = new();

        public decimal TotalAmount { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal GrandTotal => TotalAmount + ShippingFee;
    }
}
