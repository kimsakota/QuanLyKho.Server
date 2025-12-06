using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using QuanLyKho.Server.Models;

namespace QuanLyKho.Server.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class DbTestController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DbTestController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("check")]
        public IActionResult Check()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            try
            {
                using var conn = new SqlConnection(connStr);
                conn.Open();
                return Ok(new
                {
                    canConnect = true,
                    currentConnectionString = connStr  // chỉ dùng tạm để debug
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    canConnect = false,
                    error = ex.Message,
                    inner = ex.InnerException?.Message,
                    currentConnectionString = connStr
                });
            }
        }
    }


}
