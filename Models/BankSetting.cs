using System;
using System.ComponentModel.DataAnnotations;

namespace tieudaobadminton.Models
{
    public class BankSetting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn mã ngân hàng VietQR")]
        [StringLength(30)]
        public string BankCode { get; set; } = "MB"; // MB, VCB, TCB, ACB, BIDV, CTG, VPB, TPB...

        [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng")]
        [StringLength(150)]
        public string BankName { get; set; } = "MB Bank (Ngân hàng Quân Đội)";

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
        [StringLength(50)]
        public string AccountNumber { get; set; } = "0900000000";

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
        [StringLength(120)]
        public string AccountHolder { get; set; } = "TIEU DAO BADMINTON";

        [StringLength(150)]
        public string? Branch { get; set; } = "Chi nhánh Quận 7 - TP. Hồ Chí Minh";

        [Required]
        [StringLength(30)]
        public string QrTemplate { get; set; } = "compact2"; // compact2, compact, qr_only, print

        [StringLength(20)]
        public string TransferPrefix { get; set; } = "DH";

        [StringLength(300)]
        public string? Note { get; set; } = "Quét mã VietQR để tự động điền chính xác số tiền và nội dung mã đơn hàng.";

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

