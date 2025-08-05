using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CodexBackend.Data;
using CodexBackend.Models;
using System.Security.Claims;

namespace CodexBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CodexesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private string UserId => User.FindFirstValue(ClaimTypes.Name)!;

        public CodexesController(AppDbContext context) => _context = context;

        [HttpGet]
        public IActionResult Get() =>
            Ok(_context.Codexes.Where(c => c.UserId == UserId).ToList());

        [HttpPost]
        public IActionResult Create(Codex codex)
        {
            codex.Id = Guid.NewGuid().ToString();
            codex.UserId = UserId;
            codex.CreatedAt = DateTime.UtcNow;
            _context.Codexes.Add(codex);
            _context.SaveChanges();
            return Ok(codex);
        }
    }
}
