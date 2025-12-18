using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;
using EmployeeManagement.Tests.Helpers;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para DeleteEmployeeCommandHandler
/// </summary>
public class DeleteEmployeeCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<DeleteEmployeeCommandHandler>> _loggerMock;
    private readonly DeleteEmployeeCommandHandler _handler;

    public DeleteEmployeeCommandHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<DeleteEmployeeCommandHandler>();

        _handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdValido_DeveExcluirFuncionario()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        employee.IsDeleted.Should().BeTrue();
        employee.DeletedAt.Should().NotBeNull();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_DeveInvalidarCache()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Admin);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
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

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var command = new DeleteEmployeeCommand(employee.Id, Role.Admin);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        employee.IsDeleted.Should().BeTrue();
        employee.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

        var command = new DeleteEmployeeCommand(nonExistentId, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdVazio_DeveRetornarErroDeNaoEncontrado()
    {
        // Arrange
        var emptyId = Guid.Empty;

        _repositoryMock
            .Setup(x => x.GetByIdAsync(emptyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new DeleteEmployeeCommand(emptyId, Role.Admin);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Contain("NotFound");
    }

    #endregion
}