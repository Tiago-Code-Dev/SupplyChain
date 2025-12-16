using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para CreateEmployeeCommandHandler
/// </summary>
public class CreateEmployeeCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _loggerMock;
    private readonly CreateEmployeeCommandHandler _handler;

    public CreateEmployeeCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<CreateEmployeeCommandHandler>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDadosValidos_DeveCriarFuncionario()
    {
        // Arrange
        var command = CreateValidCommand();
        SetupRepositoryForNewEmployee();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(command.Email.ToLowerInvariant());

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComTelefones_DeveAdicionarTelefonesAoFuncionario()
    {
        // Arrange
        var phoneNumbers = new List<string> { "(11) 99999-8888", "(11) 88888-7777" };
        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Password123",
            Role.Employee,
            null,
            phoneNumbers,
            Role.Director);

        SetupRepositoryForNewEmployee();

        Employee? capturedEmployee = null;
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Callback<Employee, CancellationToken>((e, _) => capturedEmployee = e)
            .Returns((Employee e, CancellationToken _) => Task.FromResult(e));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEmployee.Should().NotBeNull();
        capturedEmployee!.PhoneNumbers.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveFazerHashDaSenha()
    {
        // Arrange
        var password = "MySecurePassword123";
        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            password,
            Role.Employee,
            null,
            new List<string>(),
            Role.Director);

        SetupRepositoryForNewEmployee();

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(x => x.Hash(password), Times.Once);
    }

    #endregion

    #region Validation Failures

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComEmailDuplicado_DeveRetornarErroDeConflito()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingEmployee = TestHelper.CreateValidEmployee(email: command.Email);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Conflict");
        result.Error.Description.Should().Contain("Email already exists");

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDocumentoDuplicado_DeveRetornarErroDeConflito()
    {
        // Arrange
        var command = CreateValidCommand();
        var existingEmployee = TestHelper.CreateValidEmployee(documentNumber: command.DocumentNumber);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(command.DocumentNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Document number already exists");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComRoleMaiorQueUsuarioAtual_DeveRetornarErroDeAutorizacao()
    {
        // Arrange
        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Password123",
            Role.Director,       // Tentando criar Admin
            null,
            new List<string>(),
            Role.Leader);    // Mas usuário atual é Manager

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");
        result.Error.Description.Should().Contain("cannot create an employee with a role equal to or higher");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComRoleIgualAoUsuarioAtual_DeveRetornarErroDeAutorizacao()
    {
        // Arrange
        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Password123",
            Role.Leader,     // Tentando criar Manager
            null,
            new List<string>(),
            Role.Leader);    // Usuário atual também é Manager

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComManagerIdInvalido_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            "test@test.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Password123",
            Role.Employee,
            managerId,
            new List<string>(),
            Role.Director);

        SetupRepositoryForNewEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Theory]
    [Trait("Category", "Application")]
    [InlineData("", "Last", "test@test.com")]
    [InlineData("First", "", "test@test.com")]
    [InlineData("First", "Last", "invalid-email")]
    public async Task Handle_ComDadosInvalidos_DeveRetornarErroDeValidacao(
        string firstName,
        string lastName,
        string email)
    {
        // Arrange
        var command = new CreateEmployeeCommand(
            firstName,
            lastName,
            email,
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Password123",
            Role.Employee,
            null,
            new List<string>(),
            Role.Director);

        SetupRepositoryForNewEmployee();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
    }

    #endregion

    #region Helper Methods

    private CreateEmployeeCommand CreateValidCommand()
    {
        return new CreateEmployeeCommand(
            "João",
            "Silva",
            "joao.silva@empresa.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Password123",
            Role.Employee,
            null,
            new List<string>(),
            Role.Director);
    }

    private void SetupRepositoryForNewEmployee()
    {
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns((Employee e, CancellationToken _) => Task.FromResult(e));
    }

    #endregion
}