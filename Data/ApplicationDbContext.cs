using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Models;

namespace tieudaobadminton.Data
{

    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<NewsPost> NewsPosts { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<StockReceipt> StockReceipts { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }
    }
}
