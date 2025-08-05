using CodexBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv; 
using Npgsql.EntityFrameworkCore.PostgreSQL; 

namespace CodexBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            Env.Load();

            var allowedOrigins = builder.Configuration
                .GetValue<string>("AllowedOrigins")?
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();

            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                            allowedOrigins.Length > 0
                                ? allowedOrigins
                                : new[] { "http://localhost:3000" } 
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });


            var connStr = Environment.GetEnvironmentVariable("DATABASE_URL");
            if (!string.IsNullOrEmpty(connStr))
            {
                builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connStr));
            }
            else
            {
                builder.Services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite("Data Source=codex.db"));
            }

            var jwtKey = builder.Configuration["Jwt:Key"] ?? "supersecretkey";

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

            var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
            app.Urls.Add($"http://0.0.0.0:{port}");

            app.MapControllers();

            app.Run();
        }
    }
}
