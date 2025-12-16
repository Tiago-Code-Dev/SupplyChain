using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EmployeeManagement.Tests.Integration.Employees;

/// <summary>
/// Testes de integração para criação de funcionários baseados em BDD
/// </summary>
public class CreateEmployeeTests : IntegrationTestBase
{
    public CreateEmployeeTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region BDD: Criar funcionário com dados válidos

    /// <summary>
    /// Cenário: Criar funcionário com dados válidos
    /// Dado que o usuário está autenticado como "Director"
    /// E que não existe funcionário com documento informado
    /// Quando o usuário cria um novo funcionário com dados válidos
    /// Então o sistema deve retornar status 201
    /// E o funcionário deve ter um ID único gerado
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Criar funcionário válido")]
    public async Task CreateEmployee_ComDadosValidos_DeveRetornar201()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao.silva@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1, // Employee
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.FirstName.Should().Be("João");
        result.Email.Should().Be("joao.silva@empresa.com");
    }

    #endregion

    #region BDD: Criar funcionário sem autenticação

    /// <summary>
    /// Cenário: Criar funcionário sem autenticação
    /// Dado que o usuário não está autenticado
    /// Quando o usuário tenta criar um novo funcionário
    /// Então o sistema deve retornar status 401
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Criar sem autenticação")]
    public async Task CreateEmployee_SemAutenticacao_DeveRetornar401()
    {
        // Arrange
        ClearAuthentication();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region BDD: Criar funcionário com nome vazio

    /// <summary>
    /// Cenário: Criar funcionário com nome vazio
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário com nome vazio
    /// Então o sistema deve retornar status 400
    /// E o sistema deve retornar mensagem indicando que nome é obrigatório
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação nome vazio")]
    public async Task CreateEmployee_ComNomeVazio_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Criar funcionário com email inválido

    /// <summary>
    /// Cenário: Criar funcionário com email inválido
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário com email em formato inválido
    /// Então o sistema deve retornar status 400
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação email inválido")]
    public async Task CreateEmployee_ComEmailInvalido_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "email-sem-arroba",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Criar funcionário menor de idade

    /// <summary>
    /// Cenário: Criar funcionário menor de idade
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário com menos de 18 anos
    /// Então o sistema deve retornar status 400
    /// E o sistema deve retornar mensagem "Funcionário deve ser maior de idade"
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação menor de idade")]
    public async Task CreateEmployee_MenorDeIdade_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-15), // 15 anos - menor de idade
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Criar funcionário sem telefone

    /// <summary>
    /// Cenário: Criar funcionário sem telefone
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário sem nenhum telefone
    /// Então o sistema deve retornar status 400
    /// E o sistema deve retornar mensagem "Funcionário deve possuir pelo menos um telefone"
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação sem telefone")]
    public async Task CreateEmployee_SemTelefone_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = Array.Empty<string>()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Criar funcionário com senha fraca

    /// <summary>
    /// Cenário: Criar funcionário com senha fraca
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário com senha que não atende critérios
    /// Então o sistema deve retornar status 400
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação senha fraca")]
    public async Task CreateEmployee_ComSenhaFraca_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "123", // Senha fraca
            role = 1,
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Validar formato de documento inválido

    /// <summary>
    /// Cenário: Validar formato de documento inválido
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário com documento em formato inválido
    /// Então o sistema deve retornar status 400
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação documento inválido")]
    public async Task CreateEmployee_ComDocumentoInvalido_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "123", // Documento inválido (deve ter 11 ou 14 dígitos)
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Validar formato de telefone inválido

    /// <summary>
    /// Cenário: Validar formato de telefone inválido
    /// Dado que o usuário está autenticado
    /// Quando o usuário cria um funcionário com telefone em formato inválido
    /// Então o sistema deve retornar status 400
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Validação telefone inválido")]
    public async Task CreateEmployee_ComTelefoneInvalido_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();

        var createRequest = new
        {
            firstName = "João",
            lastName = "Silva",
            email = "joao@empresa.com",
            documentNumber = "12345678901",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "123" } // Telefone inválido (deve ter 10 ou 11 dígitos)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/employees", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DTOs

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
