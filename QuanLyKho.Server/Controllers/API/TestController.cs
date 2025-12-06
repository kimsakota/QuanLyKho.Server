using Microsoft.AspNetCore.Mvc;

namespace QuanLyKho.Server.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "pong from server" });
        }
    }
}
