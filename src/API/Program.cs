using System;
using System.Text;
using System.Threading.Tasks;
using API.Middleware;
using API.Services;
using Application;
using Application.Common.Interfaces;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Clean Architecture project registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API level services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// JWT Authentication Configuration
var secretKey = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKeyForTaskManagementApplicationCleanArchitecture2026";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "TaskAppAPI";
var audience = builder.Configuration["Jwt:Audience"] ?? "TaskAppClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var repo = context.HttpContext.RequestServices.GetRequiredService<IRevokedTokenRepository>();
                if (await repo.IsRevokedAsync(token))
                {
                    context.Fail("Token has been revoked.");
                }
            }
        }
    };
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Brute-force protection for registration and login
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 10; // 10 attempts
        opt.Window = TimeSpan.FromMinutes(1); // per minute
        opt.QueueLimit = 0; // Reject immediately if limit exceeded
    });

    // General API rate limit
    options.AddSlidingWindowLimiter("global", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.SegmentsPerWindow = 4;
        opt.QueueLimit = 5;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Run Database Migrations and Seeding on Startup with a retry loop (for SQL Server container boot-up timing)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var seeder = services.GetRequiredService<DbSeeder>();

    for (int i = 1; i <= 6; i++)
    {
        try
        {
            logger.LogInformation("Running database setup and seeder (Attempt {Attempt}/6)...", i);
            await seeder.SeedAsync();
            logger.LogInformation("Database setup completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect to database on attempt {Attempt}. Retrying in 5 seconds...", i);
            if (i == 6)
            {
                logger.LogCritical(ex, "Database initialization failed after 6 attempts. Exiting.");
                throw;
            }
            await Task.Delay(5000);
        }
    }
}

app.Run();
