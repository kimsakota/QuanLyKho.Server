using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.API.Models;

namespace QuanLyKho.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers.ToListAsync();
        }

        // GET: api/Customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null) return NotFound();

            return customer;
        }

        // GET: api/Customers/Count
        [HttpGet("Count")]
        public async Task<ActionResult<int>> GetCount()
        {
            var count = await _context.Customers.CountAsync();
            return Ok(count);
        }

        //GET: api/Customers/WithTransaction?from=2024-01-01&to=2024-12-31
        [HttpGet("WithTransaction")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomersWithTransactions(DateTime from, DateTime to)
        {
            var startDate = from.Date;
            var endDate = to.Date.AddDays(1).AddTicks(-1); // Kết thúc ngày to lúc 23:59:59.9999999

            var customers = await _context.Customers
                .Where(c => _context.Exports
                    .Any(e => e.CustomerId == c.Id && e.ExportDate >= startDate && e.ExportDate <= endDate))
                .ToListAsync();

            return Ok(customers);
        }



        // POST: api/Customers
        [HttpPost]
        public async Task<ActionResult<Customer>> PostCustomer(Customer customer)
        {
            // Validate dữ liệu (Model có ObservableValidator nhưng API sẽ dùng ModelState)
            if (!ModelState.IsValid) return BadRequest(ModelState);

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCustomer", new { id = customer.Id }, customer);
        }

        // PUT: api/Customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCustomer(int id, Customer customer)
        {
            if (id != customer.Id) return BadRequest();

            _context.Entry(customer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}