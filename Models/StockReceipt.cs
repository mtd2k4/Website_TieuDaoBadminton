using System;

namespace tieudaobadminton.Models
{
    public class StockReceipt
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }

        public int QuantityAdded { get; set; }
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }

        // Nguồn gốc & Nhà cung cấp rõ ràng
        public string SupplierName { get; set; } = ""; // ví dụ: "Yonex Sunrise VN", "Victor Taiwan Distributor"
        public string? OriginCountry { get; set; }     // ví dụ: "Nhật Bản", "Đài Loan", "Việt Nam", "Trung Quốc"
        public string? BatchNumber { get; set; }       // Mã lô hàng / Số hóa đơn chứng từ

        // Số tiền nhập hàng
        public decimal UnitImportPrice { get; set; }   // Đơn giá nhập 1 chiếc/đôi (VNĐ)
        public decimal TotalImportCost { get; set; }   // Tổng tiền lô nhập = UnitImportPrice * QuantityAdded

        public string? Note { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
