using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Auth.Commands.ChangePassword;
using EmployeeManagement.Tests.Helpers;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para ChangePasswordCommandHandler
/// </summary>
public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _loggerMock;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<ChangePasswordCommandHandler>();

        _handler = new ChangePasswordCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComSenhaAtualCorreta_DeveAlterarSenha()
    {
        // Arrange
        var currentPassword = "CurrentPassword123";
        var currentPasswordHash = "hashed_current_password";
        var newPassword = "NewPassword456";
        var newPasswordHash = "hashed_new_password";

        var employee = TestHelper.CreateValidEmployee(passwordHash: currentPasswordHash);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(currentPassword, currentPasswordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.Hash(newPassword))
            .Returns(newPasswordHash);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ChangePasswordCommand(employee.Id, currentPassword, newPassword);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.PasswordHash.Should().Be(newPasswordHash);

        _repositoryMock.Verify(x => x.UpdateAsync(employee, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveFazerHashDaNovaSenha()
    {
        // Arrange
        var currentPassword = "CurrentPassword123";
        var newPassword = "NewSecurePassword789";

        var employee = TestHelper.CreateValidEmployee(passwordHash: "current_hash");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify(currentPassword, employee.PasswordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.Hash(newPassword))
            .Returns("new_hashed_password");

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ChangePasswordCommand(employee.Id, currentPassword, newPassword);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _passwordHasherMock.Verify(x => x.Hash(newPassword), Times.Once);
    }

    #endregion

    #region Failure Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComFuncionarioInexistente_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new ChangePasswordCommand(nonExistentId, "current", "new");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComSenhaAtualIncorreta_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(passwordHash: "correct_hash");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify("wrong_password", employee.PasswordHash))
            .Returns(false);

        var command = new ChangePasswordCommand(employee.Id, "wrong_password", "new_password");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");
        result.Error.Description.Should().Contain("Current password is incorrect");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComNovaSenhaVazia_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(passwordHash: "current_hash");

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _passwordHasherMock
            .Setup(x => x.Verify("current_password", employee.PasswordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns((string s) => string.IsNullOrWhiteSpace(s) ? "" : $"hashed_{s}");

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new ChangePasswordCommand(employee.Id, "current_password", "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Description.Should().Contain("Password is required");
    }

    #endregion
}