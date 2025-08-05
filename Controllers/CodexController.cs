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
    public class CodexController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CodexController(AppDbContext context) => _context = context;

        private string GetUserId() => User.FindFirstValue(ClaimTypes.Name)!;

        [HttpGet]
        public IActionResult GetAll()
        {
            var entries = _context.CodexEntries
                .Where(e => e.UserId == GetUserId())
                .ToList();
            return Ok(entries);
        }

        [HttpPost]
        public IActionResult Create(CodexEntry entry)
        {
            entry.Id = Guid.NewGuid().ToString();
            entry.UserId = GetUserId();
            entry.CreatedAt = entry.UpdatedAt = DateTime.UtcNow;

            _context.CodexEntries.Add(entry);
            _context.SaveChanges();

            return Ok(entry);
        }

        [HttpPut("{id}")]
        public IActionResult Update(string id, CodexEntry updated)
        {
            var entry = _context.CodexEntries.FirstOrDefault(e => e.Id == id && e.UserId == GetUserId());
            if (entry == null) return NotFound();

            entry.Name = updated.Name;
            entry.Description = updated.Description;
            entry.Tags = updated.Tags;
            entry.Relations = updated.Relations;
            entry.UpdatedAt = DateTime.UtcNow;

            _context.SaveChanges();
            return Ok(entry);
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(string id)
        {
            var entry = _context.CodexEntries.FirstOrDefault(e => e.Id == id && e.UserId == GetUserId());
            if (entry == null) return NotFound();

            _context.CodexEntries.Remove(entry);
            _context.SaveChanges();
            return Ok();
        }
    }
}
