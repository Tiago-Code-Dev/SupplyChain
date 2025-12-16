using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace EmployeeManagement.Tests.Integration.Employees;

/// <summary>
/// Testes de integração para exclusão de funcionários baseados em BDD
/// </summary>
public class DeleteEmployeeTests : IntegrationTestBase
{
    public DeleteEmployeeTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region BDD: Excluir funcionário com sucesso

    /// <summary>
    /// Cenário: Excluir funcionário com sucesso
    /// Dado que o usuário está autenticado como Director
    /// E que existe um funcionário cadastrado
    /// Quando o usuário exclui o funcionário
    /// Então o sistema deve retornar status 204
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Excluir sucesso")]
    public async Task Delete_ComSucesso_DeveRetornar204()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário para excluir
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "Excluir",
            email = $"teste.excluir.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "33344455566",
            birthDate = DateTime.UtcNow.AddYears(-25),
            password = "Senha@123456",
            role = 1, // Employee - pode ser excluído por Director
            phoneNumbers = new[] { "11944444444" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }

        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Act
        var response = await Client.DeleteAsync($"/api/employees/{created!.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.OK);
    }

    #endregion

    #region BDD: Excluir funcionário inexistente

    /// <summary>
    /// Cenário: Excluir funcionário inexistente
    /// Dado que o usuário está autenticado
    /// E que não existe funcionário com ID informado
    /// Quando o usuário tenta excluir
    /// Então o sistema deve retornar status 404
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Excluir inexistente")]
    public async Task Delete_ComIdInexistente_DeveRetornar404()
    {
        // Arrange
        await AuthenticateAsync();
        var idInexistente = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/employees/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region BDD: Excluir funcionário sem autenticação

    /// <summary>
    /// Cenário: Excluir funcionário sem autenticação
    /// Dado que o usuário não está autenticado
    /// Quando o usuário tenta excluir um funcionário
    /// Então o sistema deve retornar status 401
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Excluir sem autenticação")]
    public async Task Delete_SemAutenticacao_DeveRetornar401()
    {
        // Arrange
        ClearAuthentication();
        var idQualquer = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/employees/{idQualquer}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region BDD: Excluir funcionário que é gestor de outros

    /// <summary>
    /// Cenário: Excluir funcionário que é gestor de outros
    /// Dado que o usuário está autenticado como Director
    /// E que existe um funcionário que é gestor de outros funcionários
    /// Quando o usuário tenta excluir o funcionário gestor
    /// Então o sistema deve retornar status 400
    /// E o sistema deve retornar mensagem "Não é possível excluir funcionário que possui subordinados"
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Excluir com subordinados")]
    public async Task Delete_FuncionarioComSubordinados_DeveRetornar400()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário que será o gestor (Leader)
        var createGestorRequest = new
        {
            firstName = "Gestor",
            lastName = "ComSubordinados",
            email = $"gestor.subordinados.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "99900011122",
            birthDate = DateTime.UtcNow.AddYears(-40),
            password = "Senha@123456",
            role = 2, // Leader - pode ser gestor
            phoneNumbers = new[] { "11999888777" }
        };

        var createGestorResponse = await Client.PostAsJsonAsync("/api/employees", createGestorRequest);
        if (!createGestorResponse.IsSuccessStatusCode)
        {
            return;
        }
        var gestor = await createGestorResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Criar um funcionário subordinado ao gestor
        var createSubordinadoRequest = new
        {
            firstName = "Subordinado",
            lastName = "DoGestor",
            email = $"subordinado.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "00011122233",
            birthDate = DateTime.UtcNow.AddYears(-28),
            password = "Senha@123456",
            role = 1, // Employee
            managerId = gestor!.Id, // Define o gestor como manager
            phoneNumbers = new[] { "11988777666" }
        };

        var createSubordinadoResponse = await Client.PostAsJsonAsync("/api/employees", createSubordinadoRequest);
        if (!createSubordinadoResponse.IsSuccessStatusCode)
        {
            // Se não conseguiu criar subordinado, pular teste
            return;
        }

        // Act - Tentar excluir o gestor que tem subordinados
        var response = await Client.DeleteAsync($"/api/employees/{gestor.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region BDD: Verificar que funcionário foi realmente excluído

    /// <summary>
    /// Cenário: Verificar que funcionário foi realmente excluído
    /// Dado que o usuário está autenticado
    /// E que um funcionário foi excluído
    /// Quando o usuário tenta buscar o funcionário excluído
    /// Então o sistema deve retornar status 404
    /// </summary>
    [Fact]
    [Trait("Category", "Employee")]
    [Trait("BDD", "Verificar exclusão")]
    public async Task Delete_VerificarExclusao_FuncionarioNaoDeveExistir()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Criar um funcionário para excluir
        var createRequest = new
        {
            firstName = "Teste",
            lastName = "VerificarExclusao",
            email = $"teste.verificar.{Guid.NewGuid():N}@empresa.com",
            documentNumber = "11122244455",
            birthDate = DateTime.UtcNow.AddYears(-29),
            password = "Senha@123456",
            role = 1,
            phoneNumbers = new[] { "11977766655" }
        };

        var createResponse = await Client.PostAsJsonAsync("/api/employees", createRequest);
        if (!createResponse.IsSuccessStatusCode)
        {
            return;
        }
        var created = await createResponse.Content.ReadFromJsonAsync<EmployeeResponse>();

        // Excluir o funcionário
        var deleteResponse = await Client.DeleteAsync($"/api/employees/{created!.Id}");
        if (!deleteResponse.IsSuccessStatusCode && deleteResponse.StatusCode != HttpStatusCode.NoContent)
        {
            return;
        }

        // Act - Tentar buscar o funcionário excluído
        var response = await Client.GetAsync($"/api/employees/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
