using System.Reflection;
using EmployeeManagement.Application.Common.Behaviors;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // ✅ MediatR com Behaviors
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // ✅ FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // ✅ Manter AuthService (ainda não migrado para CQRS)
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}