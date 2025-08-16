using CodexBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

namespace CodexBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Load .env into Environment variables (Dev only; no-op if file missing)
            Env.Load();

            // ---------- CORS ----------
            var allowedOrigins = builder.Configuration
                .GetValue<string>("AllowedOrigins")?
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();

            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("AllowFrontend", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins.Length > 0 ? allowedOrigins : new[] { "http://localhost:3000" })
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            // ---------- Database (PG in prod if DATABASE_URL set; else SQLite local) ----------
            var connStr = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connStr));
            }
            else
            {
                var dbPath = Path.Combine(AppContext.BaseDirectory, "codex.db");
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));
            }

            // ---------- JWT ----------
            // Support either configuration "Jwt:Key" or environment "Jwt__Key"
            var jwtKey = builder.Configuration["Jwt:Key"]
                         ?? Environment.GetEnvironmentVariable("Jwt__Key");

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt:Key (or env Jwt__Key) is not configured.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ---------- Kestrel binding (single source of truth) ----------
            // Choose port: PORT env (Render/Railway) -> default 5035
            var portEnv = Environment.GetEnvironmentVariable("PORT");
            if (!int.TryParse(portEnv, out var port)) port = 5035;

            // Bind exactly one HTTP endpoint; do NOT also call UseUrls() elsewhere.
            builder.WebHost.ConfigureKestrel(o =>
            {
                o.ListenAnyIP(port);
            });

            var app = builder.Build();

            // ---------- Middleware ----------
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            // ---------- DB migrations ----------
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            // Map controllers & run
            app.MapControllers();
            app.Run();
        }
    }
}
