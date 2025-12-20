using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;
using EmployeeManagement.Tests.Helpers;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para UpdateEmployeeCommandHandler
/// </summary>
public class UpdateEmployeeCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UpdateEmployeeCommandHandler>> _loggerMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly UpdateEmployeeCommandHandler _handler;

    public UpdateEmployeeCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<UpdateEmployeeCommandHandler>();

        _handler = new UpdateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDadosValidos_DeveAtualizarFuncionario()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        var newFirstName = "Carlos";
        var newLastName = "Eduardo";
        var newEmail = "carlos.eduardo@empresa.com";

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            newFirstName,
            newLastName,
            newEmail,
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null, 
            Role.Employee 
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be(newFirstName);
        result.Value.LastName.Should().Be(newLastName);
        result.Value.Email.Should().Be(newEmail.ToLowerInvariant());

        _repositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComMesmoEmail_DeveAtualizarSemErro()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(email: "test@test.com");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Novo Nome",
            "Sobrenome",
            employee.Email,
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null,
            Role.Employee 
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _repositoryMock.Verify(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComTelefones_DeveAtualizarTelefones()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        employee.AddPhone(new PhoneNumber("(11) 99999-8888", employee.Id));

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var newPhones = new List<string> { "(11) 11111-1111", "(11) 22222-2222", "(11) 33333-3333" };

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Novo",
            "Nome",
            "novo@email.com",
            TestHelper.GenerateAdultBirthDate(),
            null,
            newPhones,
            null, 
            Role.Employee 
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.PhoneNumbers.Should().HaveCount(3);
    }

    #endregion

    #region Failure Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdInexistente_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            nonExistentId,
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null, 
            Role.Employee 
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComEmailJaExistente_DeveRetornarErroDeConflito()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(email: "original@test.com");
        var otherEmployee = TestHelper.CreateValidEmployee(email: "existing@test.com");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.EmailExistsAsync("existing@test.com", employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Test",
            "User",
            "existing@test.com",
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null, 
            Role.Employee 
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Conflict");
        result.Error.Description.Should().Contain("Email já cadastrado");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComFuncionarioComoProprioGerente_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateAdultBirthDate(),
            employee.Id, 
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null, 
            Role.Employee 
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
        result.Error.Description.Should().Contain("O funcionário não pode ser seu próprio gestor");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComGerenteInexistente_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        var nonExistentManagerId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentManagerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateAdultBirthDate(),
            nonExistentManagerId,
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null, 
            Role.Employee
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComEmailInvalido_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Test",
            "User",
            "invalid-email",
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string> { TestHelper.GenerateValidPhoneNumber() },
            null,
            Role.Employee
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SemTelefone_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            employee.Id,
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string>(), // Lista vazia - deve falhar
            null,
            Role.Employee
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
        result.Error.Description.Should().Contain("pelo menos um telefone");
    }

    #endregion
}