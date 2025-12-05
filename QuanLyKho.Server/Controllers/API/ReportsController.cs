using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.DTOs; // Đảm bảo đã namespace này chứa các DTO báo cáo
using QuanLyKho.Server.Models;

namespace QuanLyKho.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private const int LOW_STOCK_THRESHOLD = 10; // Ngưỡng cảnh báo hết hàng

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. BÁO CÁO TỒN KHO (Cho TonKhoViewModel) ---
        // GET: api/Reports/Inventory
        [HttpGet("Inventory")]
        public async Task<ActionResult<InventoryReportResponse>> GetInventoryReport()
        {
            var response = new InventoryReportResponse();

            // 1. Tính toán KPIs
            // Sử dụng AsNoTracking() để tối ưu tốc độ đọc
            response.TotalProductsCount = await _context.Products.CountAsync();

            // Tính tổng số lượng (dùng Long để tránh tràn số nếu kho quá lớn)
            // Lưu ý: EF Core sẽ dịch sang SQL SUM()
            response.TotalStockQuantity = (int)await _context.Products.SumAsync(p => (long)p.InitialQty);

            // Tính tổng giá trị (Số lượng * Giá bán)
            response.TotalStockValue = await _context.Products.SumAsync(p => p.InitialQty * p.SalePrice);

            // Đếm số sản phẩm dưới định mức
            response.LowStockCount = await _context.Products.CountAsync(p => p.InitialQty <= LOW_STOCK_THRESHOLD);

            // 2. Lấy danh sách cần nhập hàng (Top 50 ưu tiên ít hàng nhất)
            response.LowStockProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.InitialQty <= LOW_STOCK_THRESHOLD)
                .OrderBy(p => p.InitialQty)
                .Take(50)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    ProductName = p.ProductName,
                    InitialQty = p.InitialQty
                })
                .ToListAsync();

            // 3. Biểu đồ tròn: Cơ cấu giá trị theo Danh mục
            // Group theo CategoryId và tính tổng giá trị
            var catGroups = await _context.Products
                .GroupBy(p => p.CategoryId)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    TotalVal = g.Sum(p => p.InitialQty * p.SalePrice)
                })
                .OrderByDescending(x => x.TotalVal)
                .ToListAsync();

            // Lấy tên danh mục (để tránh query lồng trong GroupBy phức tạp)
            var catIds = catGroups.Where(g => g.CategoryId != null).Select(g => g.CategoryId!.Value).Distinct().ToList();
            var categories = await _context.Categories
                .Where(c => catIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

            // Xử lý Top 5 danh mục + "Khác"
            var topCats = catGroups.Take(5).ToList();
            var otherVal = catGroups.Skip(5).Sum(x => x.TotalVal);

            foreach (var group in topCats)
            {
                string catName = "Chưa phân loại";
                if (group.CategoryId.HasValue && categories.TryGetValue(group.CategoryId.Value, out var name))
                {
                    catName = name;
                }

                response.CategoryValueChart.Add(new ChartItemDto { Label = catName, Value = group.TotalVal });
            }

            if (otherVal > 0)
            {
                response.CategoryValueChart.Add(new ChartItemDto { Label = "Khác", Value = otherVal });
            }

            // 4. Biểu đồ cột: Top 5 Sản phẩm giá trị tồn kho cao nhất
            response.TopValueProductChart = await _context.Products
                .AsNoTracking()
                .OrderByDescending(p => p.InitialQty * p.SalePrice)
                .Take(5)
                .Select(p => new ChartItemDto
                {
                    Label = p.ProductName,
                    Value = p.InitialQty * p.SalePrice
                })
                .ToListAsync();

            return Ok(response);
        }

        // --- 2. BÁO CÁO KHÁCH HÀNG (Đã có) ---
        // GET: api/Reports/Customers?from=...&to=...
        [HttpGet("Customers")]
        public async Task<ActionResult<CustomerReportReponse>> GetCustomerReport(DateTime from, DateTime to)
        {
            var startDate = from.Date;
            var endDate = to.Date.AddDays(1).AddTicks(-1);

            var exportsInPeriod = _context.Exports
                .AsNoTracking()
                .Where(e => e.ExportDate >= startDate && e.ExportDate <= endDate && e.CustomerId != null)
                .Select(e => new
                {
                    e.CustomerId,
                    TotalValue = e.ExportDetails.Sum(d => d.Quantity * d.UnitPrice)
                });

            var response = new CustomerReportReponse();

            response.TotalCustomers = await _context.Customers.CountAsync();
            response.TotalOrders = await exportsInPeriod.CountAsync();

            var exportList = await exportsInPeriod.ToListAsync();
            response.ActiveCustomers = exportList.Select(e => e.CustomerId).Where(e => e.HasValue).Distinct().Count();
            response.TotalRevenue = exportList.Sum(e => e.TotalValue);

            var topStats = exportList
                .Where(e => e.CustomerId.HasValue)
                .GroupBy(e => e.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalValue)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(10)
                .ToList();

            var topCustomerIds = topStats.Select(x => x.CustomerId).ToList();
            var customerInfos = await _context.Customers
                .Where(c => topCustomerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c);

            foreach (var stat in topStats)
            {
                if (stat.CustomerId.HasValue && customerInfos.TryGetValue(stat.CustomerId.Value, out var cus))
                {
                    response.TopCustomers.Add(new TopCustomerDto
                    {
                        Name = cus.Name ?? "Khách lẻ",
                        PhoneNumber = cus.PhoneNumber ?? "--",
                        OrderCount = stat.OrderCount,
                        TotalSpent = stat.TotalSpent
                    });
                }
            }
            return Ok(response);
        }

        // --- 3. BÁO CÁO TÀI CHÍNH (Đã có) ---
        // GET: api/Reports/Financial?from=...&to=...
        [HttpGet("Financial")]
        public async Task<ActionResult<FinancialReportResponse>> GetFinancialReport(DateTime from, DateTime to)
        {
            var startDate = from.Date;
            var endDate = to.Date.AddDays(1).AddTicks(-1);

            var revenueByDay = await _context.Exports
                .AsNoTracking()
                .Where(e => e.ExportDate >= startDate && e.ExportDate <= endDate)
                .SelectMany(e => e.ExportDetails, (e, d) => new { e.ExportDate, Value = d.Quantity * d.UnitPrice })
                .GroupBy(x => x.ExportDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Value) })
                .ToDictionaryAsync(k => k.Date, v => v.Total);

            var costByDay = await _context.Imports
                .AsNoTracking()
                .Where(i => i.ImportDate >= startDate && i.ImportDate <= endDate)
                .SelectMany(i => i.ImportDetails, (i, d) => new { i.ImportDate, Value = d.Quantity * d.UnitPrice })
                .GroupBy(x => x.ImportDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(x => x.Value) })
                .ToDictionaryAsync(k => k.Date, v => v.Total);

            var response = new FinancialReportResponse();
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                decimal rev = revenueByDay.ContainsKey(currentDate) ? revenueByDay[currentDate] : 0;
                decimal cst = costByDay.ContainsKey(currentDate) ? costByDay[currentDate] : 0;

                response.DailyStats.Add(new DailyFinancialStats
                {
                    Date = currentDate,
                    Revenue = rev,
                    Cost = cst
                });

                response.TotalRevenue += rev;
                response.TotalCost += cst;

                currentDate = currentDate.AddDays(1);
            }

            response.TotalProfit = response.TotalRevenue - response.TotalCost;

            return Ok(response);
        }
    }
}