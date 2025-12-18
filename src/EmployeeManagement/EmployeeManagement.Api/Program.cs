using EmployeeManagement.Api.Configurations;
using EmployeeManagement.Api.Extensions;
using EmployeeManagement.Api.Middlewares;
using EmployeeManagement.Infrastructure.Identity;
using EmployeeManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

// Kestrel é configurado via variáveis de ambiente no Docker
// Para development local, usa as configurações do launchSettings.json

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

            var appContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var identityContext = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();

            // Verificar se é SQL Server ou InMemory
            var isSqlServer = appContext.Database.IsSqlServer();
            
            if (isSqlServer)
            {
                // Para SQL Server: criar banco e schemas manualmente
                var databaseCreator = appContext.Database.GetService<IRelationalDatabaseCreator>();
                
                // Verificar se o banco existe
                if (!await databaseCreator.ExistsAsync())
                {
                    await databaseCreator.CreateAsync();
                    logger.LogInformation("Database created");
                }

                // Criar schema 'auth' se não existir (evitando 'identity' que é palavra reservada)
                await appContext.Database.ExecuteSqlRawAsync(
                    "IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'auth') EXEC('CREATE SCHEMA auth')");
                logger.LogInformation("Schema 'auth' ensured");

                // Criar tabelas do Identity primeiro (se não existirem)
                try
                {
                    var identityCreator = identityContext.Database.GetService<IRelationalDatabaseCreator>();
                    await identityCreator.CreateTablesAsync();
                    logger.LogInformation("Identity tables created");
                }
                catch (Exception ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("There is already an object"))
                {
                    logger.LogInformation("Identity tables already exist");
                }

                // Criar tabelas do AppContext (se não existirem)
                try
                {
                    await databaseCreator.CreateTablesAsync();
                    logger.LogInformation("Employee tables created");
                }
                catch (Exception ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("There is already an object"))
                {
                    logger.LogInformation("Employee tables already exist");
                }
            }
            else
            {
                // Para InMemory: usar EnsureCreated
                await identityContext.Database.EnsureCreatedAsync();
                await appContext.Database.EnsureCreatedAsync();
                logger.LogInformation("In-memory databases created");
            }

            // Seed data
            await DbSeeder.SeedAsync(appContext, scope.ServiceProvider);
            logger.LogInformation("Employee database seeded successfully!");

            var identitySeeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
            await identitySeeder.SeedAsync();
            logger.LogInformation("Identity database seeded successfully!");

            logger.LogInformation("Database initialization completed successfully!");
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