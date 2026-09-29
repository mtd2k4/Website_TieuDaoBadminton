using tieudaobadminton.Models;

namespace tieudaobadminton.ViewModels
{
    public class HomeBrandItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Origin { get; set; } = "";
        public string Tagline { get; set; } = "";
        public int ProductCount { get; set; }
        public string AccentColor { get; set; } = "#0284c7";
        public string BadgeText { get; set; } = "CHÍNH HÃNG";
        public string LogoType { get; set; } = "general";
    }

    public class HomeVM
    {
        public List<Product> BestSellers { get; set; } = new();
        public List<NewsPost> FeaturedNews { get; set; } = new();
        public List<HomeBrandItemVM> Brands { get; set; } = new();
    }
}
