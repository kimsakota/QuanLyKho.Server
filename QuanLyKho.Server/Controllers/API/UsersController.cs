using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyKho.API.DTOs;
using QuanLyKho.API.Models;

namespace QuanLyKho.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            // Không nên trả về PasswordHash cho client
            return await _context.Users.ToListAsync();
        }

        // POST: api/Users (Tạo người dùng mới)
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
            {
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại." });
            }

            // Xử lý Hash mật khẩu trước khi lưu
            // Giả sử client gửi password dạng text trong field PasswordHash hoặc một DTO riêng
            if (!string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                // Tạm thời giữ nguyên nếu chưa cài BCrypt
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.Id }, user);
        }

        /*// POST: api/Users/Login (Đăng nhập)
        [HttpPost("Login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginRequest loginRequest)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." });
            }

            // Kiểm tra mật khẩu (So sánh Hash)
            // bool isPasswordValid = BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." });
            }

            // Trả về thông tin user (có thể kèm JWT Token nếu dùng Authentication)
            return Ok(user);
        }*/

        // PUT: api/Users/5
        // Cập nhật thông tin người dùng
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            // Logic xử lý password khi update:
            // Nếu client gửi PasswordHash mới (không rỗng), thì hash lại.
            // Nếu client để trống, ta cần giữ nguyên password cũ.
            // Tuy nhiên, với EF Core cơ bản:

            var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            if (existingUser == null) return NotFound();

            // Nếu người dùng không nhập password mới, giữ lại password cũ
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                user.PasswordHash = existingUser.PasswordHash;
            }
            else
            {
                // Nếu có password mới, thực hiện hash tại đây (nếu đã cài BCrypt)
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/Users/5
        // Xóa người dùng
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Users/5
        // Lấy thông tin chi tiết một người dùng
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // GET: api/Users/Employees
        [HttpGet("Employees")]
        public async Task<ActionResult<IEnumerable<User>>> Employees()
        {
            var employees = await _context.Users
                .Where(u => u.Role == "Employee")
                .ToListAsync();
            return Ok(employees);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        [HttpGet("CheckExists/{username}")]
        public async Task<ActionResult<bool>> CheckUserExists(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest(new { message = "Tên đăng nhập không được để trống." });
            }
            bool exists = await _context.Users.AnyAsync(u => u.Username == username);
            return Ok(exists);
        }
    }

}