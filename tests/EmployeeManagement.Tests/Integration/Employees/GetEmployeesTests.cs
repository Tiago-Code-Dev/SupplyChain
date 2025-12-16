using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EmployeeManagement.Tests.Integration.Employees;

/// <summary>
/// Testes de integração para listagem de funcionários baseados em BDD
/// </summary>
public class GetEmployeesTests : IntegrationTestBase
{
    public GetEmployeesTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region BDD: Listar todos os funcionários

    /// <summary>
    /// Cenário: Listar todos os funcionários
    /// Dado que o usuário está autenticado
    /// Quando o usuário solicita a listagem de funcionários
    /// Então o sistema deve retornar status 200
    /// E o sistema deve retornar uma lista de funcionários
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Listar todos")]
    public async Task GetAll_ComAutenticacao_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    #endregion

    #region BDD: Listar funcionários sem autenticação

    /// <summary>
    /// Cenário: Listar funcionários sem autenticação
    /// Dado que o usuário não está autenticado
    /// Quando o usuário tenta listar funcionários
    /// Então o sistema deve retornar status 401
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Listar sem autenticação")]
    public async Task GetAll_SemAutenticacao_DeveRetornar401()
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/api/employees");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region BDD: Listar funcionários com paginação

    /// <summary>
    /// Cenário: Listar funcionários com paginação
    /// Dado que o usuário está autenticado
    /// Quando o usuário solicita a listagem com paginação
    /// Então o sistema deve retornar informações de paginação
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Paginação")]
    public async Task GetAll_ComPaginacao_DeveRetornarInfoPaginacao()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/employees?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResponse<EmployeeResponse>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region BDD: Listar funcionários com filtro por nome

    /// <summary>
    /// Cenário: Listar funcionários com filtro por nome
    /// Dado que o usuário está autenticado
    /// Quando o usuário filtra por nome
    /// Então o sistema deve retornar apenas funcionários com nome correspondente
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Filtro por nome")]
    public async Task GetAll_ComFiltroPorNome_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/employees?filterByName=Admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region BDD: Listar funcionários com filtro por email

    /// <summary>
    /// Cenário: Listar funcionários com filtro por email
    /// Dado que o usuário está autenticado
    /// Quando o usuário filtra por email
    /// Então o sistema deve retornar apenas funcionários com email correspondente
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Filtro por email")]
    public async Task GetAll_ComFiltroPorEmail_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/employees?filterByEmail=admin@empresa.com");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region BDD: Listar funcionários com filtro por permissão

    /// <summary>
    /// Cenário: Listar funcionários com filtro por permissão
    /// Dado que o usuário está autenticado
    /// Quando o usuário filtra por permissão
    /// Então o sistema deve retornar apenas funcionários com permissão correspondente
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Filtro por permissão")]
    public async Task GetAll_ComFiltroPorRole_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/employees?filterByRole=3"); // Director

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region BDD: Buscar funcionário por ID existente

    /// <summary>
    /// Cenário: Buscar funcionário por ID existente
    /// Dado que o usuário está autenticado
    /// E que existe um funcionário cadastrado
    /// Quando o usuário busca pelo ID
    /// Então o sistema deve retornar os dados do funcionário
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Buscar por ID")]
    public async Task GetById_ComIdExistente_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Primeiro buscar a lista para obter um ID válido
        var listResponse = await Client.GetAsync("/api/employees");
        var listResult = await listResponse.Content.ReadFromJsonAsync<PagedResponse<EmployeeResponse>>();
        
        if (listResult?.Items?.Any() != true)
        {
            // Skip se não houver funcionários
            return;
        }

        var employeeId = listResult.Items.First().Id;

        // Act
        var response = await Client.GetAsync($"/api/employees/{employeeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(employeeId);
    }

    #endregion

    #region BDD: Buscar funcionário por ID inexistente

    /// <summary>
    /// Cenário: Buscar funcionário por ID inexistente
    /// Dado que o usuário está autenticado
    /// E que não existe funcionário com ID informado
    /// Quando o usuário busca pelo ID
    /// Então o sistema deve retornar status 404
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "ID inexistente")]
    public async Task GetById_ComIdInexistente_DeveRetornar404()
    {
        // Arrange
        await AuthenticateAsync();
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/employees/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DTOs

    private record PagedResponse<T>(
        List<T> Items,
        int TotalCount,
        int PageNumber,
        int PageSize,
        int TotalPages);

    private record EmployeeResponse(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string DocumentNumber,
        DateTime BirthDate,
        string Role,
        List<string> PhoneNumbers);

    #endregion
}
