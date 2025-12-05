using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.DTOs;
using QuanLyKho.Server.Models; // Namespace chứa AppDbContext và các Model gốc

namespace QuanLyKho.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class HistoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HistoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/History
        // Lấy danh sách tổng hợp (Nhập, Xuất, Kiểm kê)
        [HttpGet]
        public async Task<ActionResult<List<HistoryItemDto>>> GetHistory(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int type = 0, // 0=All, 1=Import, 2=Export, 3=Check
            [FromQuery] string? search = null)
        {
            var result = new List<HistoryItemDto>();

            // Xử lý ngày tháng (Default: 30 ngày gần nhất nếu null)
            var start = fromDate ?? DateTime.Now.AddDays(-30).Date;
            var end = (toDate ?? DateTime.Now).Date.AddDays(1).AddTicks(-1); // Cuối ngày

            // 1. Lấy NHẬP KHO (Nếu type = 0 hoặc 1)
            if (type == 0 || type == 1)
            {
                var imports = await _context.Imports
                    .Include(i => i.Supplier)
                    .Include(i => i.ImportDetails)
                    .Where(i => i.ImportDate >= start && i.ImportDate <= end)
                    .ToListAsync();

                result.AddRange(imports.Select(i => new HistoryItemDto
                {
                    Id = i.Id,
                    Type = "Nhập kho",
                    TransactionCode = $"IMP-{i.Id:D5}",
                    Date = i.ImportDate,
                    PartnerName = i.Supplier?.Name ?? "N/A",
                    Creator = i.ImportedBy ?? "Unknown",
                    TotalAmount = i.ImportDetails.Sum(d => d.Quantity * d.UnitPrice)
                }));
            }

            // 2. Lấy XUẤT KHO (Nếu type = 0 hoặc 2)
            if (type == 0 || type == 2)
            {
                var exports = await _context.Exports
                    .Include(e => e.Customer)
                    .Include(e => e.ExportDetails)
                    .Where(e => e.ExportDate >= start && e.ExportDate <= end)
                    .ToListAsync();

                result.AddRange(exports.Select(e => new HistoryItemDto
                {
                    Id = e.Id,
                    Type = "Xuất kho",
                    TransactionCode = $"EXP-{e.Id:D5}",
                    Date = e.ExportDate,
                    PartnerName = e.Customer?.Name ?? "Khách lẻ",
                    Creator = e.ExportedBy ?? "Unknown",
                    TotalAmount = e.ExportDetails.Sum(d => d.Quantity * d.UnitPrice)
                }));
            }

            // 3. Lấy KIỂM KÊ (Nếu type = 0 hoặc 3)
            if (type == 0 || type == 3)
            {
                var checks = await _context.InventoryChecks
                    .Where(c => c.CheckDate >= start && c.CheckDate <= end)
                    .ToListAsync();

                result.AddRange(checks.Select(c => new HistoryItemDto
                {
                    Id = c.Id,
                    Type = "Kiểm kê",
                    TransactionCode = $"CHK-{c.Id:D5}",
                    Date = c.CheckDate,
                    PartnerName = "Kiểm kê nội bộ",
                    Creator = c.CheckedBy ?? "Unknown",
                    TotalAmount = null
                }));
            }

            // 4. Lọc theo từ khóa (Search in Memory)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var key = search.Trim();
                result = result.Where(x =>
                    (x.PartnerName != null && x.PartnerName.Contains(key, StringComparison.OrdinalIgnoreCase)) ||
                    (x.TransactionCode != null && x.TransactionCode.Contains(key, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            // 5. Sắp xếp giảm dần theo ngày
            return result.OrderByDescending(x => x.Date).ToList();
        }

        // GET: api/History/Details
        // Lấy chi tiết một giao dịch cụ thể
        [HttpGet("Details")]
        public async Task<ActionResult<TransactionDetailDto>> GetDetails([FromQuery] int id, [FromQuery] string type)
        {
            var dto = new TransactionDetailDto();

            if (type == "Nhập kho")
            {
                var item = await _context.Imports
                    .Include(i => i.Supplier)
                    .Include(i => i.ImportDetails).ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (item == null) return NotFound("Phiếu nhập không tồn tại");

                dto.TransactionCode = $"IMP-{item.Id:D5}";
                dto.Type = type;
                dto.PartnerName = item.Supplier?.Name ?? "N/A";
                dto.Date = item.ImportDate;
                dto.Creator = item.ImportedBy ?? "Unknown";
                dto.TotalAmount = item.ImportDetails.Sum(d => d.Quantity * d.UnitPrice);
                dto.Details = item.ImportDetails.Select(d => new HistoryDetailItemDto
                {
                    ProductCode = d.Product?.ProductCode,
                    ProductName = d.Product?.ProductName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList();
            }
            else if (type == "Xuất kho")
            {
                var item = await _context.Exports
                    .Include(e => e.Customer)
                    .Include(e => e.ExportDetails).ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (item == null) return NotFound("Phiếu xuất không tồn tại");

                dto.TransactionCode = $"EXP-{item.Id:D5}";
                dto.Type = type;
                dto.PartnerName = item.Customer?.Name ?? "Khách lẻ";
                dto.Date = item.ExportDate;
                dto.Creator = item.ExportedBy ?? "Unknown";
                dto.TotalAmount = item.ExportDetails.Sum(d => d.Quantity * d.UnitPrice);
                dto.Details = item.ExportDetails.Select(d => new HistoryDetailItemDto
                {
                    ProductCode = d.Product?.ProductCode,
                    ProductName = d.Product?.ProductName,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice
                }).ToList();
            }
            else if (type == "Kiểm kê" || type.Contains("Kiểm"))
            {
                var item = await _context.InventoryChecks
                    .Include(c => c.Details).ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (item == null) return NotFound("Phiếu kiểm kê không tồn tại");

                dto.TransactionCode = $"CHK-{item.Id:D5}";
                dto.Type = "Kiểm kê";
                dto.PartnerName = "Kiểm kê nội bộ";
                dto.Date = item.CheckDate;
                dto.Creator = item.CheckedBy ?? "Unknown";
                dto.TotalAmount = null;
                dto.InventoryDetails = item.Details.Select(d => new InventoryDetailItemDto
                {
                    ProductCode = d.Product?.ProductCode,
                    ProductName = d.Product?.ProductName,
                    SystemQty = d.SystemQty,
                    ActualQty = d.ActualQty
                }).ToList();
            }
            else
            {
                return BadRequest("Loại phiếu không hợp lệ");
            }

            return dto;
        }
    }
}