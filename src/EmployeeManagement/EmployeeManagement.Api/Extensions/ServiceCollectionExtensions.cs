using EmployeeManagement.Api.Configurations;
using EmployeeManagement.Application;
using EmployeeManagement.Infrastructure;

namespace EmployeeManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Application & Infrastructure
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // API Configurations
        services.AddControllers();
        services.AddSwaggerConfiguration();
        services.AddCorsConfiguration(configuration);
        services.AddCompressionConfiguration();
        services.AddRateLimitingConfiguration(configuration);
        services.AddHealthCheckConfiguration(); // Sem parâmetro

        return services;
    }
}