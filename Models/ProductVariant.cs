namespace tieudaobadminton.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public string? Size { get; set; }   // 39, 40, 41, S, M, L
        public string? Color { get; set; }  // Trắng, Đen
        public string? ImageUrl { get; set; } // Ảnh preview riêng tương ứng với màu sắc

        public int Stock { get; set; }      // tồn kho riêng
        public string? Sku { get; set; }    // mã kho
    }

}
