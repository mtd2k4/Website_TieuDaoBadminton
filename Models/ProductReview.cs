using System;
using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string? UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn")]
        [StringLength(100)]
        public string UserName { get; set; } = "";

        [EmailAddress]
        [StringLength(100)]
        public string? UserEmail { get; set; }

        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        public int Rating { get; set; } = 5;

        [StringLength(200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung nhận xét")]
        [StringLength(2000)]
        public string Comment { get; set; } = "";

        public bool IsVerifiedPurchase { get; set; } = true;
        public bool IsApproved { get; set; } = true;
        public int HelpfulCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
