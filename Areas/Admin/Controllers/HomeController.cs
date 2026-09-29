using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using tieudaobadminton.Areas.Admin.ViewModels;
using tieudaobadminton.Data;
using tieudaobadminton.Models;

namespace tieudaobadminton.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _users;

        public HomeController(ApplicationDbContext db, UserManager<IdentityUser> users)
        {
            _db = db;
            _users = users;
        }

        [HttpGet]
        [Route("Admin")]
        [Route("Admin/Home")]
        [Route("Admin/Dashboard")]
        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardVM();

            // 1. KPI Metrics
            vm.TotalRevenue = await _db.Orders
                .Where(o => o.Status == "Done")
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            // Doanh thu trong tháng hiện tại
            var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            vm.MonthRevenue = await _db.Orders
                .Where(o => o.Status == "Done" && o.CreatedAt >= firstDayOfMonth)
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            vm.TotalOrders = await _db.Orders.CountAsync();
            vm.PendingOrdersCount = await _db.Orders.CountAsync(o => o.Status == "Pending");
            vm.ShippingOrdersCount = await _db.Orders.CountAsync(o => o.Status == "Shipping");
            vm.DoneOrdersCount = await _db.Orders.CountAsync(o => o.Status == "Done");
            vm.CancelledOrdersCount = await _db.Orders.CountAsync(o => o.Status == "Cancelled");

            vm.TotalProducts = await _db.Products.CountAsync(p => p.IsActive);
            vm.TotalCategories = await _db.Categories.CountAsync();
            vm.TotalBrands = await _db.Brands.CountAsync();
            vm.TotalCustomers = await _users.Users.CountAsync();
            vm.TotalNewsPosts = await _db.NewsPosts.CountAsync();

            // 5. Đối chiếu Bán & Nhập - Lãi suất (%)
            vm.TotalImportExpenses = await _db.StockReceipts.SumAsync(r => (decimal?)r.TotalImportCost) ?? 0;
            var receipts = await _db.StockReceipts.Include(r => r.Product).Where(r => r.UnitImportPrice > 0).ToListAsync();
            var doneItems = await _db.OrderItems.Include(i => i.Order).Where(i => i.Order.Status == "Done").ToListAsync();
            
            decimal costSold = 0;
            foreach (var item in doneItems)
            {
                var r = receipts.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Product != null && x.Product.Name == item.ProductName);
                decimal unitCost = r?.UnitImportPrice ?? (Math.Round((item.UnitPrice * 0.62m) / 1000m) * 1000m);
                costSold += unitCost * item.Quantity;
            }
            vm.TotalCostOfSoldGoods = costSold;
            vm.GrossProfit = vm.TotalRevenue - vm.TotalCostOfSoldGoods;
            vm.ProfitMarginPercent = vm.TotalRevenue > 0 
                ? Math.Round((double)(vm.GrossProfit / vm.TotalRevenue * 100), 1) 
                : 0;

            // 2. Recent Orders (Top 6)
            vm.RecentOrders = await _db.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .Take(6)
                .ToListAsync();

            // 3. Chart 1: Doanh thu 7 ngày gần nhất
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-6);
            var pastOrders = await _db.Orders
                .Where(o => o.CreatedAt >= sevenDaysAgo && o.Status != "Cancelled")
                .ToListAsync();

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var dayOrders = pastOrders.Where(o => o.CreatedAt.Date == day).ToList();
                vm.ChartDays.Add(day.ToString("dd/MM"));
                vm.ChartRevenue.Add(dayOrders.Sum(o => o.Total));
                vm.ChartOrderCounts.Add(dayOrders.Count);
            }

            // 4. Chart 2: Tỷ lệ phân bổ theo Hãng
            var brandGroups = await _db.Products
                .Include(p => p.Brand)
                .GroupBy(p => p.Brand != null ? p.Brand.Name : "Khác")
                .Select(g => new { Brand = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .ToListAsync();

            foreach (var bg in brandGroups)
            {
                vm.BrandLabels.Add(bg.Brand);
                vm.BrandCounts.Add(bg.Count);
            }

            // 5. Top Products Selling
            var topOrdered = await _db.OrderItems
                .GroupBy(i => i.ProductName)
                .Select(g => new { Name = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var allProducts = await _db.Products.Include(p => p.Brand).ToListAsync();
            var topList = new List<TopProductItem>();

            foreach (var item in topOrdered)
            {
                var matched = allProducts.FirstOrDefault(p => p.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase));
                topList.Add(new TopProductItem
                {
                    ProductName = item.Name,
                    TotalSold = item.TotalSold,
                    Price = matched?.SalePrice ?? matched?.Price ?? 0,
                    ImageUrl = matched?.MainImageUrl ?? "/img/demo/placeholder.jpg",
                    BrandName = matched?.Brand?.Name ?? "Tiêu Đao",
                    ProductId = matched?.Id ?? 0
                });
            }

            // Nếu DB chưa có đơn bán nào, lấy 5 sản phẩm nổi bật làm mẫu hiển thị
            if (!topList.Any())
            {
                topList = allProducts.Take(5).Select(p => new TopProductItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    ImageUrl = p.MainImageUrl ?? "/img/demo/placeholder.jpg",
                    BrandName = p.Brand?.Name ?? "Tiêu Đao",
                    Price = p.SalePrice ?? p.Price,
                    TotalSold = 8
                }).ToList();
            }

            vm.TopProducts = topList;

            return View(vm);
        }
    }
}
