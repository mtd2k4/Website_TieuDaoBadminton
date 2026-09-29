namespace tieudaobadminton.Models
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int? VariantId { get; set; } // nếu muốn có thể map tới ProductVariant
        public string ProductName { get; set; } = "";

        public string? Size { get; set; }
        public string? Color { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
