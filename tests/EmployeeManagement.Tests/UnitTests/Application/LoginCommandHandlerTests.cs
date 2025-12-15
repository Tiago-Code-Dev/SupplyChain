using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Auth.Commands.Login;
using EmployeeManagement.Tests.Helpers;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para LoginCommandHandler
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = Fixtures.MockFactory.CreateJwtServiceMock();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<LoginCommandHandler>();

        _handler = new LoginCommandHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComCredenciaisValidas_DeveRetornarAuthResponse()
    {
        // Arrange
        var email = "test@test.com";
        var password = "password123";
        var passwordHash = "hashed_password";
        var expectedToken = "valid-jwt-token";

        var employee = TestHelper.CreateValidEmployee(email: email, passwordHash: passwordHash);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(password, passwordHash))
            .Returns(true);

        _jwtServiceMock
            .Setup(x => x.GenerateToken(employee))
            .Returns(expectedToken);

        var command = new LoginCommand(email, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(expectedToken);
        result.Value.Employee.Should().NotBeNull();
        result.Value.Employee.Email.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveGerarTokenParaUsuarioValido()
    {
        // Arrange
        var email = "test@test.com";
        var password = "password123";
        var employee = TestHelper.CreateValidEmployee(email: email);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(password, employee.PasswordHash))
            .Returns(true);

        _jwtServiceMock
            .Setup(x => x.GenerateToken(employee))
            .Returns("generated-token");

        var command = new LoginCommand(email, password);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _jwtServiceMock.Verify(x => x.GenerateToken(employee), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveRetornarDataDeExpiracao()
    {
        // Arrange
        var email = "test@test.com";
        var password = "password123";
        var employee = TestHelper.CreateValidEmployee(email: email);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(password, employee.PasswordHash))
            .Returns(true);

        _jwtServiceMock
            .Setup(x => x.GenerateToken(employee))
            .Returns("token");

        var command = new LoginCommand(email, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        result.Value.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddHours(8),
            TimeSpan.FromMinutes(1));
    }

    #endregion

    #region Failure Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComEmailInexistente_DeveRetornarErroDeAutorizacao()
    {
        // Arrange
        var email = "nonexistent@test.com";
        var password = "password123";

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new LoginCommand(email, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Unauthorized");
        result.Error.Description.Should().Be("Invalid email or password");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComSenhaIncorreta_DeveRetornarErroDeAutorizacao()
    {
        // Arrange
        var email = "test@test.com";
        var password = "wrongpassword";
        var employee = TestHelper.CreateValidEmployee(email: email, passwordHash: "hashed_password");

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(password, employee.PasswordHash))
            .Returns(false);

        var command = new LoginCommand(email, password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Unauthorized");
        result.Error.Description.Should().Be("Invalid email or password");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_NaoDeveRevelarSeEmailExiste()
    {
        // Arrange
        var email = "test@test.com";
        var employee = TestHelper.CreateValidEmployee(email: email);

        // Cenário 1: Email existe, senha errada
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var resultWithWrongPassword = await _handler.Handle(
            new LoginCommand(email, "wrongpassword"),
            CancellationToken.None);

        // Cenário 2: Email não existe
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var resultWithWrongEmail = await _handler.Handle(
            new LoginCommand("wrong@test.com", "password"),
            CancellationToken.None);

        // Assert - Ambos devem ter a mesma mensagem genérica
        resultWithWrongPassword.Error.Description.Should().Be(resultWithWrongEmail.Error.Description);
        resultWithWrongPassword.Error.Description.Should().Be("Invalid email or password");
    }

    [Theory]
    [Trait("Category", "Application")]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Handle_ComEmailVazio_DeveRetornarErroDeAutorizacao(string email)
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new LoginCommand(email, "password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Unauthorized");
    }

    #endregion
}