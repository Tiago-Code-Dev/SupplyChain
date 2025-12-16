using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EmployeeManagement.Infrastructure.Persistence;
using EmployeeManagement.Infrastructure.Identity;
using Xunit;

namespace EmployeeManagement.Tests.Integration;

/// <summary>
/// Factory customizada para testes de integração
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remover DbContext existentes
            var appDbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (appDbDescriptor != null)
                services.Remove(appDbDescriptor);

            var identityDbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppIdentityDbContext>));
            if (identityDbDescriptor != null)
                services.Remove(identityDbDescriptor);

            // Adicionar DbContext em memória para testes
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

            services.AddDbContext<AppIdentityDbContext>(options =>
                options.UseInMemoryDatabase("TestIdentityDb_" + Guid.NewGuid()));

            // Build do provider e inicialização do banco
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            
            var appDb = scopedServices.GetRequiredService<AppDbContext>();
            appDb.Database.EnsureCreated();
            
            var identityDb = scopedServices.GetRequiredService<AppIdentityDbContext>();
            identityDb.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// Classe base para testes de integração
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Autentica e configura o cliente HTTP com o token JWT
    /// </summary>
    protected async Task<string> AuthenticateAsync(string email = "admin@empresa.com", string password = "Admin@123")
    {
        var loginRequest = new { email, password };
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Falha na autenticação: {response.StatusCode}");

        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        var token = result?.AccessToken ?? throw new Exception("Token não recebido");
        
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token;
    }

    /// <summary>
    /// Limpa o header de autorização
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    protected record AuthResponseDto(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        UserResponseDto User);

    protected record UserResponseDto(
        Guid Id,
        string Email,
        string FullName,
        List<string> Roles);
}
