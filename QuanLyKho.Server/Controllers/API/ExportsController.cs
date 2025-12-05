using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.DTOs; 
using QuanLyKho.Server.Models; 
using System.Security.Claims;

namespace QuanLyKho.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ExportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Exports
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Export>>> GetExports()
        {
            return await _context.Exports
                .Include(e => e.Customer)
                .Include(e => e.ExportDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(e => e.ExportDate)
                .ToListAsync();
        }

        // GET: api/Exports/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Export>> GetExport(int id)
        {
            var export = await _context.Exports
                .Include(e => e.Customer)
                .Include(e => e.ExportDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (export == null) return NotFound();

            return export;
        }

        // POST: api/Exports
        // API xử lý logic Xuất kho (Thay thế logic SaveExportAsync ở Client)
        [HttpPost]
        public async Task<ActionResult<Export>> PostExport([FromBody] CreateExportRequest request)
        {
            if (request.Details == null || request.Details.Count == 0)
                return BadRequest("Danh sách xuất kho không được để trống.");

            // Bắt đầu Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xử lý Khách hàng
                // Logic giống ViewModel: Nếu ID = 0 thì tạo mới
                int finalCustomerId = request.CustomerId;

                if (finalCustomerId == 0)
                {
                    if (string.IsNullOrWhiteSpace(request.NewCustomerName))
                        return BadRequest("Tên khách hàng không được để trống khi tạo mới.");

                    var newCustomer = new Customer
                    {
                        Name = request.NewCustomerName,
                        PhoneNumber = request.NewCustomerPhone,
                        Address = request.NewCustomerAddress
                        // Thêm các trường khác nếu cần
                    };

                    _context.Customers.Add(newCustomer);
                    await _context.SaveChangesAsync(); // Lưu để lấy ID mới
                    finalCustomerId = newCustomer.Id;
                }

                // 2. Tạo Header Phiếu Xuất
                // Lấy tên người dùng từ Token (Claims)
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                var export = new Export
                {
                    CustomerId = finalCustomerId,
                    ExportDate = DateTime.Now,
                    ExportedBy = username,
                    ExportDetails = new List<ExportDetail>()
                };

                // 3. Xử lý chi tiết & Trừ tồn kho
                foreach (var itemDto in request.Details)
                {
                    // Tìm sản phẩm trong DB (Có thể thêm AsNoTracking nếu không update, nhưng ở đây ta cần update)
                    var product = await _context.Products.FindAsync(itemDto.ProductId);

                    if (product == null)
                    {
                        throw new Exception($"Sản phẩm có ID {itemDto.ProductId} không tồn tại.");
                    }

                    // --- LOGIC KIỂM TRA TỒN KHO ---
                    if (product.InitialQty < itemDto.Quantity)
                    {
                        throw new Exception($"Sản phẩm '{product.ProductName}' không đủ hàng. Tồn: {product.InitialQty}, Yêu cầu: {itemDto.Quantity}");
                    }

                    // Trừ tồn kho
                    product.InitialQty -= itemDto.Quantity;

                    // Thêm vào danh sách chi tiết
                    var detail = new ExportDetail
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity,
                        UnitPrice = itemDto.UnitPrice
                        // ExportId sẽ tự động được gán khi EF Core lưu export cha
                    };
                    export.ExportDetails.Add(detail);
                }

                // 4. Lưu tất cả vào CSDL
                _context.Exports.Add(export);
                await _context.SaveChangesAsync();

                // 5. Commit Transaction (Xác nhận thành công)
                await transaction.CommitAsync();

                // Trả về kết quả
                return CreatedAtAction("GetExport", new { id = export.Id }, export);
            }
            catch (Exception ex)
            {
                // Nếu có lỗi, hoàn tác mọi thay đổi
                await transaction.RollbackAsync();
                // Trả về lỗi 400 kèm thông báo chi tiết để hiển thị lên Client
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/Exports/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExport(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var export = await _context.Exports
                    .Include(e => e.ExportDetails) // Include để lấy chi tiết mà trả hàng
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (export == null) return NotFound();

                // Logic hoàn trả hàng vào kho khi xóa phiếu xuất (Optional)
                foreach (var detail in export.ExportDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.InitialQty += detail.Quantity; // Cộng lại kho
                    }
                }

                _context.Exports.Remove(export);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}