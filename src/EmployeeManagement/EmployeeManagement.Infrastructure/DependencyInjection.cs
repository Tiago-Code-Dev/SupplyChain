using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Caching;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Persistence.Repositories;
using EmployeeManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // Redis Cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "EmployeeManagement:";
            });
            
            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback para cache em memória (desenvolvimento sem Redis)
            services.AddDistributedMemoryCache();
            services.AddScoped<ICacheService, RedisCacheService>();
        }

        // Unit of Work
        services.AddScoped<IUnitOfWork>(provider =>
            provider.GetRequiredService<AppDbContext>());

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        // Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
}