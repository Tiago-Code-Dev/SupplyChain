using EmployeeManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Infrastructure.Email;

/// <summary>
/// Configuração de injeção de dependência para o serviço de email
/// </summary>
public static class EmailServiceConfiguration
{
    public static IServiceCollection AddEmailService(this IServiceCollection services, IConfiguration configuration)
    {
        // Registrar configurações
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // Registrar serviço de email
        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}
