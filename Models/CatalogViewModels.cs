using tieudaobadminton.Models;

namespace tieudaobadminton.Models
{
    public class CatalogSubCategoryItem
    {
        public string Slug { get; set; } = "";
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public bool IsActive { get; set; }
    }

    public class CatalogCategoryItem
    {
        public string Slug { get; set; } = "";
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
        public int Count { get; set; }
        public bool IsActive { get; set; }
        public List<CatalogSubCategoryItem> SubCategories { get; set; } = new();
    }

    public class CatalogBrandItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Count { get; set; }
        public bool IsActive { get; set; }
    }

    public class CatalogViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<CatalogCategoryItem> Categories { get; set; } = new();
        public List<CatalogBrandItem> Brands { get; set; } = new();

        public string? ActiveCat { get; set; }
        public string? ActiveSub { get; set; }
        public int? ActiveBrandId { get; set; }
        public string? ActivePrice { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public string? ActiveSort { get; set; }
        public string? SearchKeyword { get; set; }

        public string CurrentTitle { get; set; } = "Tất cả danh mục sản phẩm";
        public string? CurrentParentName { get; set; }
        public string? CurrentSubName { get; set; }
        public int TotalCount { get; set; }
    }
}
