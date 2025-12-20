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
    private readonly Mock<ICacheService> _cacheMock;
    private readonly CreateEmployeeCommandHandler _handler;

    public CreateEmployeeCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<CreateEmployeeCommandHandler>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        _handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDadosValidos_DeveCriarFuncionario()
    {
        // Arrange
        var command = CreateValidCommand(new List<string> { "11999999999" });
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
            new List<string> { "11999999999" },
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
        var command = CreateValidCommand(new List<string> { "11999999999" });
        var existingEmployee = TestHelper.CreateValidEmployee(email: command.Email);

        _repositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Conflict");
        result.Error.Description.Should().Contain("Email");

        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDocumentoDuplicado_DeveRetornarErroDeConflito()
    {
        // Arrange
        var command = CreateValidCommand(new List<string> { "11999999999" });
        var existingEmployee = TestHelper.CreateValidEmployee(documentNumber: command.DocumentNumber);

        _repositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.DocumentExistsAsync(command.DocumentNumber, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Documento");
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
            Role.Director,      
            null,
            new List<string> { "11999999999" },
            Role.Leader);    

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");
        result.Error.Description.Should().Contain("Você não pode criar um funcionário com permissão igual ou superior à sua");
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
            Role.Leader,    
            null,
            new List<string> { "11999999999" },
            Role.Leader);    

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
            new List<string> { "11999999999" },
            Role.Director);

        SetupRepositoryForNewEmployee();

        _repositoryMock
            .Setup(x => x.ExistsAsync(managerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

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
            new List<string> { "11999999999" },
            Role.Director);

        SetupRepositoryForNewEmployee();

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
        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            "test@test.com",
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
        result.Error.Description.Should().Contain("Funcionário deve possuir pelo menos um telefone");
    }

    #endregion

    #region Helper Methods

    private CreateEmployeeCommand CreateValidCommand()
    {
        return CreateValidCommand(new List<string> { "11999999999" });
    }

    private CreateEmployeeCommand CreateValidCommand(List<string> phones)
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
            phones,
            Role.Director);
    }

    private void SetupRepositoryForNewEmployee()
    {
        _repositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.DocumentExistsAsync(It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns((Employee e, CancellationToken _) => Task.FromResult(e));
    }

    #endregion
}