using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;

namespace EmployeeManagement.Tests.UnitTests.Application;

/// <summary>
/// Testes unitários para GetAllEmployeesQueryHandler
/// </summary>
public class GetAllEmployeesQueryHandlerTests
{
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheMock;
    private readonly Mock<ILogger<GetAllEmployeesQueryHandler>> _loggerMock;
    private readonly GetAllEmployeesQueryHandler _handler;

    public GetAllEmployeesQueryHandlerTests()
    {
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _cacheMock = new Mock<ICacheService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetAllEmployeesQueryHandler>();

        _handler = new GetAllEmployeesQueryHandler(
            _repositoryMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    #region Success Cases - Basic Listing

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SemFiltros_DeveRetornarListaPaginada()
    {
        // Arrange
        var employees = new List<Employee>
        {
            TestHelper.CreateValidEmployee(firstName: "João"),
            TestHelper.CreateValidEmployee(firstName: "Maria"),
            TestHelper.CreateValidEmployee(firstName: "Pedro")
        };

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((employees, employees.Count));

        var query = new GetAllEmployeesQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    #endregion

    #region Success Cases - Filtering

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComSearchTerm_DevePassarParaRepositorio()
    {
        // Arrange
        var searchTerm = "João";
        string? capturedSearchTerm = null;

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, string?, Role?, Guid?, string?, bool, CancellationToken>(
                (page, size, search, name, email, role, managerId, sortBy, sortDesc, ct) =>
                    capturedSearchTerm = search)
            .ReturnsAsync((new List<Employee>(), 0));

        var query = new GetAllEmployeesQuery(1, 10, searchTerm);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedSearchTerm.Should().Be(searchTerm);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComFiltroDeRole_DevePassarParaRepositorio()
    {
        // Arrange
        var filterRole = Role.Director;
        Role? capturedRole = null;

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, string?, Role?, Guid?, string?, bool, CancellationToken>(
                (page, size, search, name, email, role, managerId, sortBy, sortDesc, ct) =>
                    capturedRole = role)
            .ReturnsAsync((new List<Employee>(), 0));

        var query = new GetAllEmployeesQuery(1, 10, FilterByRole: filterRole);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedRole.Should().Be(filterRole);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComFiltroDeManagerId_DevePassarParaRepositorio()
    {
        // Arrange
        var managerId = Guid.NewGuid();
        Guid? capturedManagerId = null;

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, string?, Role?, Guid?, string?, bool, CancellationToken>(
                (page, size, search, name, email, role, mgrId, sortBy, sortDesc, ct) =>
                    capturedManagerId = mgrId)
            .ReturnsAsync((new List<Employee>(), 0));

        var query = new GetAllEmployeesQuery(1, 10, FilterByManagerId: managerId);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedManagerId.Should().Be(managerId);
    }

    #endregion

    #region Success Cases - Sorting

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComOrdenacao_DevePassarParaRepositorio()
    {
        // Arrange
        var sortBy = "FirstName";
        var sortDescending = true;
        string? capturedSortBy = null;
        bool capturedSortDesc = false;

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Callback<int, int, string?, string?, string?, Role?, Guid?, string?, bool, CancellationToken>(
                (page, size, search, name, email, role, mgrId, sort, sortDesc, ct) =>
                {
                    capturedSortBy = sort;
                    capturedSortDesc = sortDesc;
                })
            .ReturnsAsync((new List<Employee>(), 0));

        var query = new GetAllEmployeesQuery(1, 10, SortBy: sortBy, SortDescending: sortDescending);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedSortBy.Should().Be(sortBy);
        capturedSortDesc.Should().BeTrue();
    }

    #endregion

    #region Success Cases - Cache Behavior

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SemFiltros_DeveUsarCache()
    {
        // Arrange
        var employees = new List<Employee> { TestHelper.CreateValidEmployee() };

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((employees, 1));

        var query = new GetAllEmployeesQuery(1, 10);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert 
        _cacheMock.Verify(
            x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _cacheMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<EmployeeResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComFiltros_NaoDeveUsarCache()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Employee>(), 0));

        var query = new GetAllEmployeesQuery(1, 10, SearchTerm: "filtro");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        _cacheMock.Verify(
            x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _cacheMock.Verify(
            x => x.SetAsync(It.IsAny<string>(), It.IsAny<PagedResult<EmployeeResponse>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_ComDadosEmCache_NaoDeveChamarRepositorio()
    {
        // Arrange
        var cachedResult = PagedResult<EmployeeResponse>.Create(
            new List<EmployeeResponse> { EmployeeResponse.FromEntity(TestHelper.CreateValidEmployee()) },
            1, 1, 10);

        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var query = new GetAllEmployeesQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().Be(cachedResult);

        _repositoryMock.Verify(
            x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("Category", "Application")]
    public async Task Handle_SemResultados_DeveRetornarListaVazia()
    {
        // Arrange
        _cacheMock
            .Setup(x => x.GetAsync<PagedResult<EmployeeResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResult<EmployeeResponse>?)null);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<Role?>(), It.IsAny<Guid?>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Employee>(), 0));

        var query = new GetAllEmployeesQuery(1, 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    #endregion
}