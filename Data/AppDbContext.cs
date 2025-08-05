using Microsoft.EntityFrameworkCore;
using CodexBackend.Models;

namespace CodexBackend.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Codex> Codexes => Set<Codex>();
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<CodexEntry> CodexEntries => Set<CodexEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CodexEntry>().Ignore(e => e.Fields);
        }
    }
}
