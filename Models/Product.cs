namespace tieudaobadminton.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int BrandId { get; set; }
        public Brand Brand { get; set; } = null!;

        public decimal Price { get; set; }       // giá niêm yết
        public decimal? SalePrice { get; set; }  // giá khuyến mãi

        public string? Description { get; set; }
        public string? MainImageUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public List<ProductImage> Images { get; set; } = new();

        public List<ProductVariant> Variants { get; set; } = new();
        public List<ProductReview> Reviews { get; set; } = new();
    }

}
