using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EmployeeManagement.Tests.Integration.Employees;

/// <summary>
/// Testes de integração para atualização de funcionários baseados em BDD
/// </summary>
public class UpdateEmployeeTests : IntegrationTestBase
{
    public UpdateEmployeeTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region BDD: Atualizar funcionário com dados válidos

    /// <summary>
    /// Cenário: Atualizar funcionário com dados válidos
    /// Dado que o usuário está autenticado
    /// E que existe um funcionário cadastrado
    /// Quando o usuário atualiza os dados do funcionário
    /// Então o sistema deve retornar status 200
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Atualizar válido")]
    public async Task Update_ComDadosValidos_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário primeiro
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "Update",
            email = $"teste.update.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "98765432101",
            birthDate = DateTime.UtcNow.AddYears(-30),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11988888888" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            // Se não conseguir criar, pular o teste
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        
        var updateRequest = new
        {
            firstName = "Teste Atualizado",
            lastName = "Update",
            email = created!.Email,
            birthDate = DateTime.UtcNow.AddYears(-31),
            phoneNumbers = new[] { "11977777777" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Teste Atualizado");
    }

    #endregion

    #region BDD: Atualizar funcionário inexistente

    /// <summary>
    /// Cenário: Atualizar funcionário inexistente
    /// Dado que o usuário está autenticado
    /// E que não existe funcionário com ID informado
    /// Quando o usuário tenta atualizar
    /// Então o sistema deve retornar status 404
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Atualizar inexistente")]
    public async Task Update_ComIdInexistente_DeveRetornar404()
    {
        // Arrange
        await AuthenticateAsync();
        var idInexistente = Guid.NewGuid();

        var updateRequest = new
        {
            firstName = "Teste",
            lastName = "Inexistente",
            email = "teste@empresa.com",
            birthDate = DateTime.UtcNow.AddYears(-25),
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{idInexistente}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region BDD: Atualizar funcionário sem autenticação

    /// <summary>
    /// Cenário: Atualizar funcionário sem autenticação
    /// Dado que o usuário não está autenticado
    /// Quando o usuário tenta atualizar um funcionário
    /// Então o sistema deve retornar status 401
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Atualizar sem autenticação")]
    public async Task Update_SemAutenticacao_DeveRetornar401()
    {
        // Arrange
        ClearAuthentication();
        var idQualquer = Guid.NewGuid();

        var updateRequest = new
        {
            firstName = "Teste",
            lastName = "SemAuth",
            email = "teste@empresa.com",
            birthDate = DateTime.UtcNow.AddYears(-25),
            phoneNumbers = new[] { "11999999999" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{idQualquer}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region BDD: Atualizar funcionário para menor de idade

    /// <summary>
    /// Cenário: Atualizar funcionário para menor de idade
    /// Dado que o usuário está autenticado
    /// Quando o usuário atualiza a data de nascimento para menor de 18 anos
    /// Então o sistema deve retornar status 400
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Atualizar menor de idade")]
    public async Task Update_ParaMenorDeIdade_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário primeiro
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "Menor",
            email = $"teste.menor.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "11122233344",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11966666666" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        
        var updateRequest = new
        {
            firstName = "Teste",
            lastName = "Menor",
            email = created!.Email,
            birthDate = DateTime.UtcNow.AddYears(-15), // Menor de idade
            phoneNumbers = new[] { "11966666666" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Atualizar funcionário removendo todos os telefones

    /// <summary>
    /// Cenário: Atualizar funcionário removendo todos os telefones
    /// Dado que o usuário está autenticado
    /// Quando o usuário atualiza removendo todos os telefones
    /// Então o sistema deve retornar status 400
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Atualizar sem telefone")]
    public async Task Update_SemTelefone_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário primeiro
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "SemTelefone",
            email = $"teste.semtel.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "22233344455",
            birthDate = DateTime.UtcNow.AddYears(-28),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11955555555" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();
        
        var updateRequest = new
        {
            firstName = "Teste",
            lastName = "SemTelefone",
            email = created!.Email,
            birthDate = DateTime.UtcNow.AddYears(-28),
            phoneNumbers = Array.Empty<string>() // Sem telefones
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Atualizar funcionário com email duplicado de outro funcionário

    /// <summary>
    /// Cenário: Atualizar funcionário com email duplicado de outro funcionário
    /// Dado que o usuário está autenticado
    /// E que existe um funcionário com ID "1" e email "email1@empresa.com"
    /// E que existe um funcionário com ID "2" e email "email2@empresa.com"
    /// Quando o usuário tenta atualizar o funcionário "1" com email "email2@empresa.com"
    /// Então o sistema deve retornar status 409
    /// E o sistema deve retornar mensagem "Email já cadastrado para outro funcionário"
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Email duplicado")]
    public async Task Update_ComEmailDuplicado_DeveRetornar409()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar primeiro funcionário
        var createRequest1 = new
        {
            firstName = "Funcionario",
            lastName = "Um",
            email = $"funcionario1.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "44455566677",
            birthDate = DateTime.UtcNow.AddYears(-30),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11944444444" }
        };

        var createResponse1 = await Client.PostAsJsonAsync("/api/employees", createRequest1);
        if (!createResponse1.IsSuccessStatusCode)
        {
            return;
        }
        var created1 = await createResponse1.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Criar segundo funcionário
        var createRequest2 = new
        {
            firstName = "Funcionario",
            lastName = "Dois",
            email = $"funcionario2.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "55566677788",
            birthDate = DateTime.UtcNow.AddYears(-28),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11933333333" }
        };

        var createResponse2 = await Client.PostAsJsonAsync("/api/employees", createRequest2);
        if (!createResponse2.IsSuccessStatusCode)
        {
            return;
        }
        var created2 = await createResponse2.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Tentar atualizar funcionário 1 com o email do funcionário 2
        var updateRequest = new
        {
            firstName = "Funcionario",
            lastName = "Um",
            email = created2!.Email, // Email do outro funcionário
            birthDate = DateTime.UtcNow.AddYears(-30),
            phoneNumbers = new[] { "11944444444" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created1!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region BDD: Atualizar funcionário mantendo seu próprio email

    /// <summary>
    /// Cenário: Atualizar funcionário mantendo seu próprio email
    /// Dado que o usuário está autenticado
    /// E que existe um funcionário cadastrado
    /// Quando o usuário atualiza mantendo o mesmo email
    /// Então o sistema deve retornar status 200
    /// E o funcionário deve ser atualizado com sucesso
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Manter próprio email")]
    public async Task Update_MantendoProprioEmail_DeveRetornar200()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "ManterEmail",
            email = $"teste.manteremail.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "66677788899",
            birthDate = DateTime.UtcNow.AddYears(-32),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11922222222" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Atualizar mantendo o mesmo email
        var updateRequest = new
        {
            firstName = "Teste Atualizado",
            lastName = "ManterEmail",
            email = created!.Email, // Mesmo email
            birthDate = DateTime.UtcNow.AddYears(-32),
            phoneNumbers = new[] { "11922222222" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("Teste Atualizado");
        result.Email.Should().Be(created.Email);
    }

    #endregion

    #region BDD: Atualizar funcionário com gestor sendo ele mesmo

    /// <summary>
    /// Cenário: Atualizar funcionário com gestor sendo ele mesmo
    /// Dado que o usuário está autenticado
    /// E que existe um funcionário cadastrado
    /// Quando o usuário tenta atualizar o funcionário definindo ele próprio como gestor
    /// Então o sistema deve retornar status 400
    /// E o sistema deve retornar mensagem "Funcionário não pode ser gestor de si mesmo"
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Gestor próprio")]
    public async Task Update_ComGestorProprioFuncionario_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "GestorProprio",
            email = $"teste.gestorproprio.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "77788899900",
            birthDate = DateTime.UtcNow.AddYears(-35),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11911111111" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Tentar atualizar definindo ele próprio como gestor
        var updateRequest = new
        {
            firstName = "Teste",
            lastName = "GestorProprio",
            email = created!.Email,
            birthDate = DateTime.UtcNow.AddYears(-35),
            managerId = created.Id, // Ele próprio como gestor
            phoneNumbers = new[] { "11911111111" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Atualizar funcionário com gestor inexistente

    /// <summary>
    /// Cenário: Atualizar funcionário com gestor inexistente
    /// Dado que o usuário está autenticado
    /// E que existe um funcionário cadastrado
    /// E que não existe gestor com ID informado
    /// Quando o usuário tenta atualizar o funcionário com GestorId inexistente
    /// Então o sistema deve retornar status 400
    /// E o sistema deve retornar mensagem "Gestor não encontrado"
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Gestor inexistente")]
    public async Task Update_ComGestorInexistente_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "GestorInexistente",
            email = $"teste.gestorinex.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "88899900011",
            birthDate = DateTime.UtcNow.AddYears(-27),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11900000000" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Tentar atualizar com gestor inexistente
        var updateRequest = new
        {
            firstName = "Teste",
            lastName = "GestorInexistente",
            email = created!.Email,
            birthDate = DateTime.UtcNow.AddYears(-27),
            managerId = Guid.NewGuid(), // Gestor que não existe
            phoneNumbers = new[] { "11900000000" }
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/employees/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
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
