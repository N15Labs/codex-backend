using Microsoft.AspNetCore.Mvc;

namespace CodexBackend.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new {
            status = "ok",
            timestamp = DateTime.UtcNow.ToString("o")
        });
    }
}
