using Asp.Versioning;

namespace EmployeeManagement.Api.Configurations;

/// <summary>
/// Configuração de versionamento da API
/// </summary>
public static class ApiVersioningConfiguration
{
    /// <summary>
    /// Versão padrão da API
    /// </summary>
    public static readonly ApiVersion DefaultVersion = new(1, 0);

    /// <summary>
    /// Adiciona configuração de versionamento
    /// </summary>
    public static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // Versão padrão quando não especificada
            options.DefaultApiVersion = DefaultVersion;
            
            // Assume versão padrão se não informada
            options.AssumeDefaultVersionWhenUnspecified = true;
            
            // Reporta versões suportadas no header da resposta
            options.ReportApiVersions = true;
            
            // Lê a versão do URL, Query String e Header
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new QueryStringApiVersionReader("api-version"),
                new HeaderApiVersionReader("X-Api-Version"));
        })
        .AddApiExplorer(options =>
        {
            // Formato: 'v'major[.minor][-status]
            options.GroupNameFormat = "'v'VVV";
            
            // Substitui a versão no URL
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }
}