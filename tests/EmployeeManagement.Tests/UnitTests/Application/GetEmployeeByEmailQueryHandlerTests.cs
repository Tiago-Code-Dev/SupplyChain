using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeByEmail;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para GetEmployeeByEmailQueryHandler
/// </summary>
public class GetEmployeeByEmailQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<GetEmployeeByEmailQueryHandler>> _loggerMock;
    private readonly GetEmployeeByEmailQueryHandler _handler;

    public GetEmployeeByEmailQueryHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetEmployeeByEmailQueryHandler>();

        _handler = new GetEmployeeByEmailQueryHandler(
            _repositoryMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComEmailExistente_DeveRetornarFuncionario()
    {
        // Arrange
        var email = "joao.silva@empresa.com";
        var employee = TestHelper.CreateValidEmployee(email: email);

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var query = new GetEmployeeByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email.ToLowerInvariant());
        result.FirstName.Should().Be(employee.FirstName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDadosEmCache_NaoDeveChamarRepositorio()
    {
        // Arrange
        var email = "cached@empresa.com";
        var employee = TestHelper.CreateValidEmployee(email: email);
        var cachedResponse = EmployeeResponse.FromEntity(employee);

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResponse);

        var query = new GetEmployeeByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email.ToLowerInvariant());

        _repositoryMock.Verify(
            x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [Trait("Category", "Application")]
    [InlineData("TEST@EMPRESA.COM")]
    [InlineData("Test@Empresa.Com")]
    [InlineData("test@empresa.com")]
    public async Task Handle_DeveBuscarEmailCaseInsensitive(string email)
    {
        // Arrange
        var normalizedEmail = email.ToLowerInvariant();
        var employee = TestHelper.CreateValidEmployee(email: normalizedEmail);

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employee);

        var query = new GetEmployeeByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(normalizedEmail);
    }

    #endregion

    #region Failure Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComEmailInexistente_DeveRetornarNull()
    {
        // Arrange
        var email = "naoexiste@empresa.com";

        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var query = new GetEmployeeByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [Trait("Category", "Application")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_ComEmailVazioOuEspacos_DeveRetornarNull(string email)
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetOrSetAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<Task<EmployeeResponse?>>, TimeSpan, CancellationToken>(
                async (key, factory, ttl, ct) => await factory());

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var query = new GetEmployeeByEmailQuery(email);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}