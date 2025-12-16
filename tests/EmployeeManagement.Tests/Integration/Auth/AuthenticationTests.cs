using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EmployeeManagement.Tests.Integration.Auth;

/// <summary>
/// Testes de integração para autenticação baseados em BDD
/// </summary>
public class AuthenticationTests : IntegrationTestBase
{
    public AuthenticationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region BDD: Login bem-sucedido com credenciais válidas

    /// <summary>
    /// Cenário: Login bem-sucedido com credenciais válidas
    /// Dado que existe um funcionário cadastrado no sistema
    /// Quando o usuário realiza login com credenciais válidas
    /// Então o sistema deve retornar status 200
    /// E o sistema deve retornar um token JWT válido
    /// </summary>
    [Fact]
    [Trait("Category", "Auth")]
    [Trait("BDD", "Login bem-sucedido")]
    public async Task Login_ComCredenciaisValidas_DeveRetornar200ComToken()
    {
        // Arrange
        var loginRequest = new
        {
            email = "admin@empresa.com",
            password = "Admin@123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("admin@empresa.com");
    }

    #endregion

    #region BDD: Login falha com email inválido

    /// <summary>
    /// Cenário: Login falha com email inválido
    /// Dado que não existe um funcionário cadastrado com email informado
    /// Quando o usuário realiza login
    /// Então o sistema deve retornar status 401
    /// E o sistema deve retornar mensagem "Credenciais inválidas"
    /// </summary>
    [Fact]
    [Trait("Category", "Auth")]
    [Trait("BDD", "Login falha email inválido")]
    public async Task Login_ComEmailInexistente_DeveRetornar401()
    {
        // Arrange
        var loginRequest = new
        {
            email = "inexistente@empresa.com",
            password = "Senha@123456"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result.Should().NotBeNull();
        result!.Error.Should().Contain("Credenciais inválidas");
    }

    #endregion

    #region BDD: Login falha com senha incorreta

    /// <summary>
    /// Cenário: Login falha com senha incorreta
    /// Dado que existe um funcionário cadastrado no sistema
    /// Quando o usuário realiza login com senha errada
    /// Então o sistema deve retornar status 401
    /// E o sistema deve retornar mensagem "Credenciais inválidas"
    /// </summary>
    [Fact]
    [Trait("Category", "Auth")]
    [Trait("BDD", "Login falha senha incorreta")]
    public async Task Login_ComSenhaIncorreta_DeveRetornar401()
    {
        // Arrange
        var loginRequest = new
        {
            email = "admin@empresa.com",
            password = "SenhaErrada123"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var result = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        result.Should().NotBeNull();
        result!.Error.Should().Contain("Credenciais inválidas");
    }

    #endregion

    #region BDD: Acesso negado sem token de autenticação

    /// <summary>
    /// Cenário: Acesso negado sem token de autenticação
    /// Dado que o usuário não possui token de autenticação
    /// Quando o usuário tenta acessar o endpoint GET /api/employees
    /// Então o sistema deve retornar status 401
    /// </summary>
    [Fact]
    [Trait("Category", "Auth")]
    [Trait("BDD", "Acesso negado sem token")]
    public async Task AcessarEndpointProtegido_SemToken_DeveRetornar401()
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region BDD: Refresh Token

    /// <summary>
    /// Cenário: Renovar token com refresh token válido
    /// Dado que o usuário possui um refresh token válido
    /// Quando o usuário solicita renovação do token
    /// Então o sistema deve retornar novos tokens
    /// </summary>
    [Fact]
    [Trait("Category", "Auth")]
    [Trait("BDD", "Refresh token válido")]
    public async Task RefreshToken_ComTokenValido_DeveRetornarNovosTokens()
    {
        // Arrange - Fazer login primeiro
        var loginRequest = new { email = "admin@empresa.com", password = "Admin@123" };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRequest = new { refreshToken = loginResult!.RefreshToken };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe(loginResult.RefreshToken, "Token deve ser rotacionado");
    }

    #endregion

    #region DTOs para Testes

    private record AuthResponse(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        UserResponse User);

    private record UserResponse(
        Guid Id,
        string Email,
        string FullName,
        List<string> Roles);

    private record ErrorResponse(string Error);

    #endregion
}
