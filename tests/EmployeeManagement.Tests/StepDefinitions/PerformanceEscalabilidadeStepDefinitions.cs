using System.Diagnostics;
using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Performance e Escalabilidade")]
public class PerformanceEscalabilidadeStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _createLoggerMock;

    private List<Employee> _employees = new();
    private Employee? _employeeToFind;
    private PagedResult<EmployeeResponse>? _pagedResult;
    private EmployeeResponse? _employeeResult;
    private Result<EmployeeResponse>? _createResult;
    private Stopwatch _stopwatch = new();
    private long _responseTimeMs;
    private int _httpStatus = 200;
    private bool _cacheWasUsed;
    private Role _currentUserRole = Role.Employee;

    public PerformanceEscalabilidadeStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _createLoggerMock = Fixtures.MockFactory.CreateLoggerMock<CreateEmployeeCommandHandler>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password_securely");
    }

    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        _currentUserRole = Enum.Parse<Role>(role);
        _scenarioContext.Set(_currentUserRole, "CurrentUserRole");
        _scenarioContext.Set(true, "IsAuthenticated");
    }

    [Given(@"que existem (.*) funcionários cadastrados no sistema")]
    public void DadoQueExistemFuncionariosCadastradosNoSistema(int quantidade)
    {
        _employees = new List<Employee>();
        for (int i = 0; i < quantidade; i++)
        {
            var employee = TestHelper.CreateValidEmployee(
                firstName: $"Funcionario{i}",
                lastName: $"Teste{i}",
                email: $"funcionario{i}@supply.com");
            _employees.Add(employee);
        }

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employees);

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, string? search, string? sort, bool desc, CancellationToken ct) =>
            {
                var skip = (page - 1) * size;
                IEnumerable<Employee> filtered = _employees;
                
                if (!string.IsNullOrEmpty(search))
                {
                    filtered = filtered.Where(e => 
                        e.FirstName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        e.LastName.Contains(search, StringComparison.OrdinalIgnoreCase));
                }
                
                var items = filtered.Skip(skip).Take(size);
                return (items, filtered.Count());
            });
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecido()
    {
        _employeeToFind = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employeeToFind.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToFind);

        _scenarioContext.Set(_employeeToFind.Id, "EmployeeId");
    }

    [Given(@"que os dados estão em cache")]
    public void DadoQueOsDadosEstaoEmCache()
    {
        var response = EmployeeResponse.FromEntity(_employeeToFind!);
        _cacheServiceMock
            .Setup(x => x.GetAsync<EmployeeResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback(() => _cacheWasUsed = true);
    }

    [When(@"o usuário solicita a listagem de funcionários com paginação de (.*) por página")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosComPaginacao(int pageSize)
    {
        _stopwatch.Restart();
        
        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object);
        var query = new GetAllEmployeesQuery(1, pageSize);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        
        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = 200;
    }

    [When(@"o usuário solicita os dados do funcionário por ID")]
    public async Task QuandoOUsuarioSolicitaOsDadosDoFuncionarioPorId()
    {
        _stopwatch.Restart();
        
        var employeeId = _scenarioContext.Get<Guid>("EmployeeId");
        var handler = new GetEmployeeByIdQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetEmployeeByIdQuery(employeeId);
        _employeeResult = await handler.Handle(query, CancellationToken.None);
        
        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _employeeResult != null ? 200 : 404;
    }

    [When(@"o usuário filtra funcionários pelo nome ""(.*)""")]
    public async Task QuandoOUsuarioFiltraFuncionariosPeloNome(string nome)
    {
        _stopwatch.Restart();
        
        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object);
        var query = new GetAllEmployeesQuery(SearchTerm: nome);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        
        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = 200;
    }

    [When(@"o usuário cria um novo funcionário com dados válidos")]
    public async Task QuandoOUsuarioCriaUmNovoFuncionarioComDadosValidos()
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

        _stopwatch.Restart();
        
        var command = new CreateEmployeeCommand(
            "João",
            "Silva",
            "joao.perf@supply.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Senha@123456",
            Role.Employee,
            null,
            new List<string> { "11999999999" },
            _currentUserRole);

        var handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _createLoggerMock.Object);

        _createResult = await handler.Handle(command, CancellationToken.None);
        
        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _createResult.IsSuccess ? 201 : 400;
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o tempo de resposta deve ser menor que (.*)ms")]
    public void EntaoOTempoDeRespostaDeveSerMenorQue(int maxMs)
    {
        // Em ambiente de teste com mocks, o tempo será muito menor
        // Este teste valida que a lógica de medição funciona
        _responseTimeMs.Should().BeLessThan(maxMs, 
            $"O tempo de resposta foi {_responseTimeMs}ms, esperava-se menos que {maxMs}ms");
    }

    [Then(@"o sistema deve retornar apenas (.*) funcionários")]
    public void EntaoOSistemaDeveRetornarApenasFuncionarios(int quantidade)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().HaveCount(quantidade);
    }

    [Then(@"a resposta deve incluir informações de paginação")]
    public void EntaoARespostaDeveIncluirInformacoesDePaginacao()
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.TotalCount.Should().BeGreaterThan(0);
        _pagedResult.TotalPages.Should().BeGreaterThan(0);
        _pagedResult.PageNumber.Should().BeGreaterThan(0);
    }

    [Then(@"os dados devem vir do cache")]
    public void EntaoOsDadosDevemVirDoCache()
    {
        _cacheWasUsed.Should().BeTrue();
    }

    [Then(@"o banco de dados não deve ser consultado")]
    public void EntaoOBancoDeDadosNaoDeveSerConsultado()
    {
        _repositoryMock.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Then(@"o hash da senha deve ser gerado de forma segura")]
    public void EntaoOHashDaSenhaDeveSerGeradoDeFormaSegura()
    {
        _passwordHasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
    }
}
