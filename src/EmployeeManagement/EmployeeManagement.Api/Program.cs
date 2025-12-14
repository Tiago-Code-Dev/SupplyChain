using EmployeeManagement.Api.Configurations;
using EmployeeManagement.Api.Extensions;
using EmployeeManagement.Api.Middlewares;
using EmployeeManagement.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Middleware pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger sempre habilitado (ou apenas em Development)
app.UseSwaggerConfiguration();

app.UseResponseCompression();
app.UseCorsConfiguration();
app.UseHealthCheckConfiguration();

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Database initialization
await InitializeDatabaseAsync(app);

app.Run();

async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);

    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogInformation("Attempting to create database... (attempt {Attempt}/{MaxRetries})", i + 1, maxRetries);

            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database created successfully!");

            await DbSeeder.SeedAsync(context, scope.ServiceProvider);
            logger.LogInformation("Database seeded successfully!");

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