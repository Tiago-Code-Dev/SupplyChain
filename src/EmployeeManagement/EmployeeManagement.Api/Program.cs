using EmployeeManagement.Api.Configurations;
using EmployeeManagement.Api.Extensions;
using EmployeeManagement.Api.Middlewares;
using EmployeeManagement.Infrastructure.Identity;
using EmployeeManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Middleware pipeline - ordem importante!

// 1. Correlation ID (primeiro para rastrear todas as requisições)
app.UseCorrelationId();

// 2. Exception Handling
app.UseMiddleware<GlobalExceptionMiddleware>();

// 3. Swagger
app.UseSwaggerConfiguration();

// 4. Compression
app.UseResponseCompression();

// 5. CORS
app.UseCorsConfiguration();

// 6. Health Checks
app.UseHealthCheckConfiguration();

// 7. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 8. Rate Limiting
app.UseRateLimiter();

// 9. Map Controllers
app.MapControllers();

// Database initialization
await InitializeDatabaseAsync(app);

app.Run();

async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to initialize databases... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

            // Employee Database
            var appContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await appContext.Database.EnsureCreatedAsync();
            logger.LogInformation("Employee database created successfully!");

            await DbSeeder.SeedAsync(appContext, scope.ServiceProvider);
            logger.LogInformation("Employee database seeded successfully!");

            // Identity Database
            var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await identitySeeder.SeedAsync();
            logger.LogInformation("Identity database seeded successfully!");

            break;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Database not ready, retrying in {Delay}s... ({Attempt}/{MaxRetries})",
                delay.TotalSeconds, i + 1, maxRetries);

            if (i == maxRetries - 1)
            {
                logger.LogError(ex, "Failed to initialize database after {MaxRetries} attempts", maxRetries);
                throw;
            }

            await Task.Delay(delay);
        }
    }
}

// Classe parcial para permitir WebApplicationFactory nos testes de integração
public partial class Program { }