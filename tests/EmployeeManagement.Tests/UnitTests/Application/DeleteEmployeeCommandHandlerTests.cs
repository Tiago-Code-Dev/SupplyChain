using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para DeleteEmployeeCommandHandler
/// </summary>
public class DeleteEmployeeCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<DeleteEmployeeCommandHandler>> _loggerMock;
    private readonly DeleteEmployeeCommandHandler _handler;

    public DeleteEmployeeCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<DeleteEmployeeCommandHandler>();

        _handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Configura os mocks necessários para uma exclusão bem-sucedida
    /// </summary>
    private void SetupSuccessfulDeletion(Employee employee)
    {
        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        _repositoryMock
            .Setup(x => x.HasSubordinatesAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.SoftDeleteAsync(employee.Id, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                employee.Delete();
            })
            .Returns(Task.CompletedTask);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdValido_DeveExcluirFuncionario()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        SetupSuccessfulDeletion(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
        employee.DeletedAt.Should().NotBeNull();

        // Verifica que SoftDeleteAsync foi chamado
        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(employee.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveInvalidarCache()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        SetupSuccessfulDeletion(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Admin);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveUsarSoftDelete()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        SetupSuccessfulDeletion(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Admin);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.IsDeleted.Should().BeTrue();
        employee.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Failure Cases - Not Found

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdInexistente_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new DeleteEmployeeCommand(nonExistentId, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");

        // Verifica que SoftDeleteAsync NÃO foi chamado
        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdVazio_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var emptyId = Guid.Empty;

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(emptyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new DeleteEmployeeCommand(emptyId, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion

    #region Permission Cases - Initial Role Check (Role < Leader)

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComRoleEmployee_DeveRetornarForbidden()
    {
        // Arrange
        var targetEmployee = TestHelper.CreateValidEmployee(role: Role.Employee);
        var command = new DeleteEmployeeCommand(targetEmployee.Id, Role.Employee);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");

        _repositoryMock.Verify(
            x => x.GetByIdForDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Permission Cases - Hierarchy (Leader)

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_LeaderDeletandoEmployee_DeveRetornarSucesso()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(role: Role.Employee);
        SetupSuccessfulDeletion(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Leader);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_LeaderDeletandoLeader_DeveRetornarForbidden()
    {
        // Arrange
        var targetLeader = TestHelper.CreateValidEmployee(role: Role.Leader);

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(targetLeader.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetLeader);

        var command = new DeleteEmployeeCommand(targetLeader.Id, Role.Leader);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");

        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_LeaderDeletandoDirector_DeveRetornarForbidden()
    {
        // Arrange
        var targetDirector = TestHelper.CreateValidEmployee(role: Role.Director);

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(targetDirector.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetDirector);

        var command = new DeleteEmployeeCommand(targetDirector.Id, Role.Leader);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");

        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Permission Cases - Hierarchy (Director)

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DirectorDeletandoEmployee_DeveRetornarSucesso()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee(role: Role.Employee);
        SetupSuccessfulDeletion(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Director);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DirectorDeletandoLeader_DeveRetornarSucesso()
    {
        // Arrange
        var leader = TestHelper.CreateValidEmployee(role: Role.Leader);
        SetupSuccessfulDeletion(leader);

        var command = new DeleteEmployeeCommand(leader.Id, Role.Director);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        leader.IsDeleted.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DirectorDeletandoDirector_DeveRetornarForbidden()
    {
        // Arrange
        var targetDirector = TestHelper.CreateValidEmployee(role: Role.Director);

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(targetDirector.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetDirector);

        var command = new DeleteEmployeeCommand(targetDirector.Id, Role.Director);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Forbidden");

        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Business Rules - Subordinates

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_FuncionarioComSubordinados_DeveRetornarErroDeValidacao()
    {
        // Arrange
        var manager = TestHelper.CreateValidEmployee(role: Role.Leader);

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(manager.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);

        _repositoryMock
            .Setup(x => x.HasSubordinatesAsync(manager.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DeleteEmployeeCommand(manager.Id, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("Validation");

        _repositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}