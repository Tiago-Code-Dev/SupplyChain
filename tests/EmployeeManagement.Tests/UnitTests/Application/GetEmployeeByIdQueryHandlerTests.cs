using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para GetEmployeeByIdQueryHandler
/// </summary>
public class GetEmployeeByIdQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<GetEmployeeByIdQueryHandler>> _loggerMock;
    private readonly GetEmployeeByIdQueryHandler _handler;

    public GetEmployeeByIdQueryHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetEmployeeByIdQueryHandler>();

        _handler = new GetEmployeeByIdQueryHandler(
            _repositoryMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdExistente_DeveRetornarFuncionario()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByIdAsync(employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var query = new GetEmployeeByIdQuery(employee.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(employee.Id);
        result.Email.Should().Be(employee.Email);
        result.FirstName.Should().Be(employee.FirstName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDadosEmCache_NaoDeveChamarRepositorio()
    {
        // Arrange
        var employee = TestHelper.CreateValidEmployee();
        var cachedResponse = EmployeeResponse.FromEntity(employee);

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var query = new GetEmployeeByIdQuery(employee.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(employee.Id);

        _repositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComIdInexistente_DeveRetornarNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var query = new GetEmployeeByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComGuidVazio_DeveRetornarNull()
    {
        // Arrange
        var emptyId = Guid.Empty;

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByIdAsync(emptyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var query = new GetEmployeeByIdQuery(emptyId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}