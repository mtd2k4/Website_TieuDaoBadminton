using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.ViewModels
{
    public class StockReceiptFormVM
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm cần nhập")]
        public int ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng"), Range(1, 100000, ErrorMessage = "Số lượng nhập phải từ 1 chiếc trở lên")]
        public int Quantity { get; set; } = 20;

        [Required(ErrorMessage = "Vui lòng nhập nguồn gốc hoặc nhà cung cấp")]
        [StringLength(200)]
        public string SupplierName { get; set; } = "";

        public string? OriginCountry { get; set; }
        public string? BatchNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn giá nhập"), Range(1000, 999999999, ErrorMessage = "Đơn giá nhập không hợp lệ")]
        public decimal UnitImportPrice { get; set; }

        public string? Note { get; set; }

        // Dropdown helpers
        public List<SelectListItem> ProductsList { get; set; } = new();
        public List<SelectListItem> VariantsList { get; set; } = new();
    }

    public class ProductProfitReportItemVM
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string CategoryName { get; set; } = "";
        public string BrandName { get; set; } = "";

        public decimal RetailPrice { get; set; }        // Giá bán (SalePrice hoặc Price)
        public decimal UnitCostPrice { get; set; }       // Đơn giá nhập gần nhất (Giá vốn)
        public decimal UnitProfit { get; set; }          // Lãi trên 1 chiếc = RetailPrice - UnitCostPrice
        public double ProfitMarginPercent { get; set; }  // % Lãi trên giá bán = UnitProfit / RetailPrice * 100
        public double MarkupPercent { get; set; }        // % Lãi trên vốn nhập = UnitProfit / UnitCostPrice * 100

        public int TotalImportedUnits { get; set; }     // Tổng số lượng đã nhập
        public int TotalSoldUnits { get; set; }         // Tổng số lượng đã bán
        public int CurrentStock { get; set; }           // Tồn kho hiện tại

        public decimal TotalRevenue { get; set; }        // Doanh thu = TotalSoldUnits * RetailPrice
        public decimal TotalCostSold { get; set; }       // Giá vốn hàng bán = TotalSoldUnits * UnitCostPrice
        public decimal GrossProfit { get; set; }         // Lợi nhuận gộp thực tế = TotalRevenue - TotalCostSold
    }

    public class ProfitComparisonSummaryVM
    {
        // 4 Thẻ KPI toàn hệ thống
        public decimal TotalRevenue { get; set; }
        public decimal TotalImportExpenses { get; set; }
        public decimal TotalCostOfGoodsSold { get; set; }
        public decimal TotalGrossProfit { get; set; }
        public double AverageMarginPercent { get; set; }
        public double AverageMarkupPercent { get; set; }

        // Bộ lọc & Tìm kiếm
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public string? MarginFilter { get; set; }
        public string? SortBy { get; set; } = "revenue_desc";
        public List<SelectListItem> CategoriesList { get; set; } = new();

        // Phân trang
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;

        // Danh sách sản phẩm của trang hiện tại
        public List<ProductProfitReportItemVM> ReportItems { get; set; } = new();
    }
}
