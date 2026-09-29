namespace tieudaobadminton.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }

        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }

        public string? Size { get; set; }
        public string? Color { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}
