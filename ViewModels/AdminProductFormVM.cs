using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;

namespace tieudaobadminton.ViewModels
{
    public class AdminProductFormVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm"), StringLength(200)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int BrandId { get; set; }

        [Range(0, 999999999, ErrorMessage = "Giá không hợp lệ")]
        public decimal Price { get; set; }

        public decimal? SalePrice { get; set; }

        public string? MainImageUrl { get; set; }
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        // ===== QUẢN LÝ KHO HÀNG & TỒN KHO =====
        [Range(0, 999999, ErrorMessage = "Số lượng tồn kho không hợp lệ")]
        public int InitialStock { get; set; } = 15;

        public string? Sku { get; set; }

        // Nhập nhanh phân loại khi tạo mới (nếu là giày hoặc quần áo)
        public string? InitialSizes { get; set; }   // ví dụ: "39, 40, 41, 42, 43" hoặc "S, M, L, XL"
        public string? InitialColors { get; set; }  // ví dụ: "Trắng, Đen"

        // Danh sách biến thể tồn kho hiện tại (dùng khi Edit)
        public List<ProductVariantItemVM> ExistingVariants { get; set; } = new();

        // Dropdown data
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Brands { get; set; } = new();
        public IFormFile? ImageFile { get; set; }
    }

    public class ProductVariantItemVM
    {
        public int Id { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public int Stock { get; set; }
        public string? Sku { get; set; }
    }
}
