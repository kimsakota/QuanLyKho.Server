using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.Server.Models;

namespace QuanLyKho.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products
                .Include(p => p.Category) // Join bảng Category để lấy tên danh mục
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return product;
        }

        // GET: api/Products/5//LastImportPrice
        [HttpGet("{id}/LastImportPrice")]
        public async Task<ActionResult<decimal>> GetLastImportPrice(int id)
        {
            // Tìm dòng nhập hàng gần nhất của sản phẩm này
            var lastImportDetail = await _context.ImportDetails
                .Include(d => d.Import)
                .Where(d => d.ProductId == id)
                .OrderByDescending(d => d.Import!.ImportDate)
                .FirstOrDefaultAsync();

            // Nếu tìm thấy thì trả về giá cũ, không thì trả về 0
            return Ok(lastImportDetail?.UnitPrice ?? 0);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            // Kiểm tra trùng mã sản phẩm
            if (await _context.Products.AnyAsync(p => p.ProductCode == product.ProductCode))
            {
                return BadRequest(new { message = "Mã sản phẩm đã tồn tại." });
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.Id }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.Id) return BadRequest();

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // 1. Kiểm tra xem sản phẩm có trong lịch sử nhập hàng không
            var hasImport = await _context.ImportDetails.AnyAsync(x => x.ProductId == id);
            if (hasImport)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm này vì đã tồn tại trong lịch sử nhập hàng." });
            }

            // 2. Kiểm tra xem sản phẩm có trong lịch sử xuất hàng không
            var hasExport = await _context.ExportDetails.AnyAsync(x => x.ProductId == id);
            if (hasExport)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm này vì đã tồn tại trong lịch sử xuất hàng." });
            }

            // 3. Kiểm tra xem sản phẩm có trong phiếu kiểm kê không (Nên kiểm tra thêm cái này để đảm bảo toàn vẹn)
            var hasInventoryCheck = await _context.InventoryCheckDetails.AnyAsync(x => x.ProductId == id);
            if (hasInventoryCheck)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm này vì đã tồn tại trong phiếu kiểm kê." });
            }

            // Nếu không có ràng buộc nào, tiến hành xóa
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Products/CheckExists?code=SP-001
        [HttpGet("CheckExists")]
        public async Task<ActionResult<bool>> CheckProductExists([FromQuery] string code)
        {
            if(string.IsNullOrEmpty(code))
                return BadRequest(new { message = "Mã sản phẩm không được để trống." });

            var exists = await _context.Products.AnyAsync(p => p.ProductCode == code);
            return Ok(exists);
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}