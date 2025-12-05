using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.DTOs; // Nhớ using namespace chứa DTO
using QuanLyKho.Server.Models;

namespace QuanLyKho.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ImportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ImportsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Imports
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Import>>> GetImports()
        {
            return await _context.Imports
                .Include(i => i.Supplier)
                .Include(i => i.ImportDetails)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(i => i.ImportDate)
                .ToListAsync();
        }

        // GET: api/Imports/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Import>> GetImport(int id)
        {
            var importModel = await _context.Imports
                .Include(i => i.Supplier)
                .Include(i => i.ImportDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (importModel == null) return NotFound();

            return importModel;
        }

        // Lấy lần nhập giá gần nhất của 1 sản phẩm
        // GET: api/Imports/LastTransaction/5 (5 là ProductId)
        [HttpGet("LastTransaction/{productId}")]
        public async Task<ActionResult<ImportDetail>> GetLastImport(int productId)
        {
            var lastImport = await _context.ImportDetails
                .Include(d => d.Import)
                .Where(d => d.ProductId == productId)
                .OrderByDescending(d => d.Import!.ImportDate)
                .FirstOrDefaultAsync();

            // Nếu chưa nhập lần nào thì trả về NotFound hoặc null để Client xử lý
            if (lastImport == null) return NotFound();

            return lastImport;
        }

        // POST: api/Imports
        // Cập nhật: Nhận DTO CreateImportRequest thay vì Entity Import
        [HttpPost]
        public async Task<ActionResult<Import>> PostImport([FromBody] CreateImportRequest request)
        {
            // 1. Validate dữ liệu
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Map từ DTO sang Entity Import
                var importEntity = new Import
                {
                    SupplierId = request.SupplierId,
                    ImportDate = request.ImportDate,
                    ImportedBy = request.ImportedBy,
                    ImportDetails = new List<ImportDetail>()
                };

                // 3. Xử lý từng chi tiết phiếu nhập
                foreach (var item in request.Details)
                {
                    // Tạo ImportDetail
                    var detail = new ImportDetail
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };
                    importEntity.ImportDetails.Add(detail);

                    // --- CẬP NHẬT TỒN KHO ---
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        throw new Exception($"Sản phẩm ID {item.ProductId} không tồn tại");
                    }

                    // Cộng dồn số lượng vào tồn kho
                    product.InitialQty += item.Quantity;

                    // (Tùy chọn) Cập nhật giá nhập mới nhất vào bảng Product nếu muốn
                    // product.CostPrice = item.UnitPrice; 
                }

                // 4. Lưu vào Database
                _context.Imports.Add(importEntity);
                await _context.SaveChangesAsync();

                // Commit transaction nếu mọi thứ ổn
                await transaction.CommitAsync();

                // Trả về kết quả (201 Created)
                return CreatedAtAction("GetImport", new { id = importEntity.Id }, importEntity);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "Lỗi khi tạo phiếu nhập: " + ex.Message });
            }
        }

        // DELETE: api/Imports/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImport(int id)
        {
            var importModel = await _context.Imports
                                    .Include(i => i.ImportDetails) // Load chi tiết để trừ lại kho
                                    .FirstOrDefaultAsync(i => i.Id == id);

            if (importModel == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Khi xóa phiếu nhập -> Phải TRỪ lại tồn kho (Rollback kho)
                foreach (var detail in importModel.ImportDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.InitialQty -= detail.Quantity;
                    }
                }

                _context.Imports.Remove(importModel);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { message = "Lỗi khi xóa phiếu nhập: " + ex.Message });
            }
        }
    }
}