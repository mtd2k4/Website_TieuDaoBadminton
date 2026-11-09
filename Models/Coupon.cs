using System;
using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã giảm giá")]
        [StringLength(30)]
        public string Code { get; set; } = ""; // VD: TIEUDAO50, YONEX10

        [Required(ErrorMessage = "Vui lòng nhập mô tả chương trình")]
        [StringLength(200)]
        public string Description { get; set; } = "";

        [Required]
        public string DiscountType { get; set; } = "Fixed"; // "Fixed" (VNĐ) hoặc "Percent" (%)

        [Range(1, 100000000, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
        public decimal DiscountValue { get; set; } // VD: 50000 hoặc 10 (%)

        public decimal? MaxDiscountAmount { get; set; } // Mức giảm tối đa khi DiscountType == "Percent"

        public decimal MinOrderValue { get; set; } = 0; // Đơn hàng tối thiểu để áp dụng

        public int UsageLimit { get; set; } = 100; // Tổng số lượt cho phép

        public int UsedCount { get; set; } = 0; // Số lượt đã dùng

        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(3);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

