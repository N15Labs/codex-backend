using CodexBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using System.Linq;

namespace CodexBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Env.Load();

            var allowedOriginsConfig = builder.Configuration["AllowedOrigins"]
                                     ?? Environment.GetEnvironmentVariable("AllowedOrigins");
            var allowedOrigins = (allowedOriginsConfig ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("AllowFrontend", policy =>
                    policy.SetIsOriginAllowed(origin =>
                    {
                        if (string.IsNullOrWhiteSpace(origin)) return false;
                        if (allowedOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase))) return true;
                        var host = new Uri(origin).Host;
                        return host.Equals("codex-50f3xxj1b-s022511hs-projects.vercel.app", StringComparison.OrdinalIgnoreCase)
                               || host.Equals("codex-six-teal.vercel.app", StringComparison.OrdinalIgnoreCase)
                               || host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
                               || host.Equals("n15labs.co.uk", StringComparison.OrdinalIgnoreCase)
                               || host.EndsWith(".n15labs.co.uk", StringComparison.OrdinalIgnoreCase)
                               || host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                );
            });

            var connStr = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrWhiteSpace(connStr))
            {
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connStr));
            }
            else
            {
                var sqlitePath = Environment.GetEnvironmentVariable("SQLITE_PATH");
                var dbPath = !string.IsNullOrWhiteSpace(sqlitePath)
                    ? sqlitePath
                    : Path.Combine(AppContext.BaseDirectory, "codex.db");
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));
            }

            var jwtKey = builder.Configuration["Jwt:Key"]
                         ?? Environment.GetEnvironmentVariable("Jwt__Key");

            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("Jwt:Key (or env Jwt__Key) is not configured.");

            var jwtIssuer = builder.Configuration["Jwt:Issuer"]
                            ?? Environment.GetEnvironmentVariable("Jwt__Issuer");
            var jwtAudience = builder.Configuration["Jwt:Audience"]
                              ?? Environment.GetEnvironmentVariable("Jwt__Audience");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = !string.IsNullOrWhiteSpace(jwtIssuer),
                        ValidateAudience = !string.IsNullOrWhiteSpace(jwtAudience),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var portEnv = Environment.GetEnvironmentVariable("PORT");
            if (!int.TryParse(portEnv, out var port)) port = 5035;

            builder.WebHost.ConfigureKestrel(o =>
            {
                o.ListenAnyIP(port);
            });

            var app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            app.MapControllers();
            app.Run();
        }
    }
}
