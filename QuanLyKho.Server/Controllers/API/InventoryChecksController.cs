using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.DTOs; // Sử dụng DTO
using QuanLyKho.Server.Models; // Sử dụng Entity
using System.Security.Claims;

namespace QuanLyKho.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryChecksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryChecksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/InventoryChecks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryCheck>>> GetInventoryChecks()
        {
            return await _context.InventoryChecks
                .Include(c => c.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(c => c.CheckDate)
                .ToListAsync();
        }

        // GET: api/InventoryChecks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryCheck>> GetInventoryCheck(int id)
        {
            var check = await _context.InventoryChecks
                .Include(c => c.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (check == null) return NotFound();

            return check;
        }

        // POST: api/InventoryChecks
        // Thay đổi: Dùng CreateInventoryCheckRequest thay vì InventoryCheck model trực tiếp
        [HttpPost]
        public async Task<IActionResult> PostInventoryCheck([FromBody] CreateInventoryCheckRequest request)
        {
            if (request.Details == null || request.Details.Count == 0)
                return BadRequest("Danh sách kiểm kê không được trống.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy người dùng hiện tại từ Token
                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

                // 2. Tạo Header phiếu kiểm kê
                var checkHeader = new InventoryCheck
                {
                    CheckDate = request.CheckDate,
                    CheckedBy = username,
                    Notes = request.Notes,
                    Details = new List<InventoryCheckDetail>()
                };

                // 3. Xử lý chi tiết và CẬP NHẬT TỒN KHO
                foreach (var itemDto in request.Details)
                {
                    var product = await _context.Products.FindAsync(itemDto.ProductId);
                    if (product == null)
                        throw new Exception($"Sản phẩm ID {itemDto.ProductId} không tồn tại.");

                    // QUAN TRỌNG: Cập nhật tồn kho trong DB bằng số lượng thực tế kiểm được
                    product.InitialQty = itemDto.ActualQty;

                    // Thêm vào chi tiết phiếu để lưu lịch sử
                    checkHeader.Details.Add(new InventoryCheckDetail
                    {
                        ProductId = itemDto.ProductId,
                        SystemQty = itemDto.SystemQty, // Lưu lại số tồn trên phần mềm trước khi kiểm
                        ActualQty = itemDto.ActualQty  // Lưu lại số thực tế
                    });
                }

                _context.InventoryChecks.Add(checkHeader);

                // Lưu thay đổi (bao gồm InventoryCheck, InventoryCheckDetail và update Product)
                await _context.SaveChangesAsync();

                // Commit Transaction
                await transaction.CommitAsync();

                return Ok(new { Message = "Cân bằng kho thành công", Id = checkHeader.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { Message = ex.Message });
            }
        }

        // DELETE: api/InventoryChecks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInventoryCheck(int id)
        {
            var check = await _context.InventoryChecks.FindAsync(id);
            if (check == null) return NotFound();

            // Lưu ý: Xóa phiếu kiểm kê thường không hoàn tác lại tồn kho 
            // vì tồn kho đã thay đổi theo thực tế tại thời điểm kiểm.
            _context.InventoryChecks.Remove(check);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}