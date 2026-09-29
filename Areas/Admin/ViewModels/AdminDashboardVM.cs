using System;
using System.Collections.Generic;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.ViewModels
{
    public class AdminDashboardVM
    {
        // 1. KPI Metrics
        public decimal TotalRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrdersCount { get; set; }
        public int ShippingOrdersCount { get; set; }
        public int DoneOrdersCount { get; set; }
        public int CancelledOrdersCount { get; set; }

        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalBrands { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalNewsPosts { get; set; }

        // 2. Recent & Top Lists
        public List<Order> RecentOrders { get; set; } = new();
        public List<TopProductItem> TopProducts { get; set; } = new();

        // 3. Chart Data: 7 Days Revenue
        public List<string> ChartDays { get; set; } = new();
        public List<decimal> ChartRevenue { get; set; } = new();
        public List<int> ChartOrderCounts { get; set; } = new();

        // 4. Chart Data: Brand Distribution
        public List<string> BrandLabels { get; set; } = new();
        public List<int> BrandCounts { get; set; } = new();

        // 5. Đối chiếu Bán & Nhập - Lãi suất (%)
        public decimal TotalImportExpenses { get; set; }  // Tổng tiền vốn đã chi nhập kho
        public decimal TotalCostOfSoldGoods { get; set; } // Giá vốn của các đơn hàng đã bán
        public decimal GrossProfit { get; set; }          // Lợi nhuận gộp thực thu
        public double ProfitMarginPercent { get; set; }   // % Tỷ suất lãi suất bán hàng
    }

    public class TopProductItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? BrandName { get; set; }
        public decimal Price { get; set; }
        public int TotalSold { get; set; }
    }
}
