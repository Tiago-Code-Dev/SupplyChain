using System.Diagnostics;
using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;
using EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeByEmail;
using EmployeeManagement.Application.Features.Auth.Commands.Login;

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
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _createLoggerMock;
    private readonly Mock<ILogger<UpdateEmployeeCommandHandler>> _updateLoggerMock;
    private readonly Mock<ILogger<DeleteEmployeeCommandHandler>> _deleteLoggerMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loginLoggerMock;

    private List<Employee> _employees = new();
    private Employee? _employeeToFind;
    private Employee? _employeeToDelete;
    private PagedResult<EmployeeResponse>? _pagedResult;
    private EmployeeResponse? _employeeResult;
    private Result<EmployeeResponse>? _createResult;
    private Result<EmployeeResponse>? _updateResult;
    private Result? _deleteResult;
    private Result<AuthResponse>? _loginResult;
    private Stopwatch _stopwatch = new();
    private long _responseTimeMs;
    private int _httpStatus = 200;
    private bool _cacheWasUsed;
    private Role _currentUserRole = Role.Employee;
    private string _loginEmail = string.Empty;
    private string _loginPassword = string.Empty;

    public PerformanceEscalabilidadeStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = Fixtures.MockFactory.CreateJwtServiceMock();
        _identityServiceMock = new Mock<IIdentityService>();
        _createLoggerMock = Fixtures.MockFactory.CreateLoggerMock<CreateEmployeeCommandHandler>();
        _updateLoggerMock = Fixtures.MockFactory.CreateLoggerMock<UpdateEmployeeCommandHandler>();
        _deleteLoggerMock = Fixtures.MockFactory.CreateLoggerMock<DeleteEmployeeCommandHandler>();
        _loginLoggerMock = Fixtures.MockFactory.CreateLoggerMock<LoginCommandHandler>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password_securely");
    }

    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        _currentUserRole = Enum.Parse<Role>(role);
        _scenarioContext.Set(_currentUserRole, "CurrentUserRole");
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecido()
    {
        _employeeToFind = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employeeToFind.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToFind);

        _cacheServiceMock
            .Setup(x => x.GetOrSetAsync<EmployeeResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, Func<Task<EmployeeResponse?>> factory, TimeSpan? expiration, CancellationToken ct)
                => factory().Result);

        _scenarioContext.Set(_employeeToFind.Id, "EmployeeId");
    }

    [Given(@"que existem (.*) funcionários cadastrados no sistema")]
    public void DadoQueExistemFuncionariosCadastradosNoSistema(int quantidade)
    {
        _employees = new List<Employee>();
        for (int i = 0; i < quantidade; i++)
        {
            _employees.Add(TestHelper.CreateValidEmployee());
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
                It.IsAny<string?>(),
                It.IsAny<Role?>(),
                It.IsAny<Guid?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int page, int size, string? search, string? filterName, string? filterEmail,
                           Role? filterRole, Guid? filterMgr, string? sort, bool desc, CancellationToken ct) =>
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
                return (items, _employees.Count);
            });
    }

    [Given(@"que os dados estão em cache")]
    public void DadoQueOsDadosEstaoEmCache()
    {
        var response = EmployeeResponse.FromEntity(_employeeToFind!);
        _cacheServiceMock
            .Setup(x => x.GetOrSetAsync<EmployeeResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback(() => _cacheWasUsed = true);
    }

    [When(@"o usuário solicita a listagem de funcionários com paginação de (.*) por página")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosComPaginacao(int pageSize)
    {
        _stopwatch.Restart();

        var loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetAllEmployeesQueryHandler>();
        var handler = new GetAllEmployeesQueryHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            loggerMock.Object);

        var query = new GetAllEmployeesQuery(1, pageSize);
        _pagedResult = await handler.Handle(query, CancellationToken.None);

        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = 200;
    }

    [When(@"o usuário solicita a listagem com paginação de (.*) por página")]
    public async Task QuandoOUsuarioSolicitaAListagemComPaginacao(int pageSize)
    {
        _stopwatch.Restart();

        var loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetAllEmployeesQueryHandler>();
        var handler = new GetAllEmployeesQueryHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            loggerMock.Object);

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
        var loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetEmployeeByIdQueryHandler>();
        var handler = new GetEmployeeByIdQueryHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            loggerMock.Object);

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

        var loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetAllEmployeesQueryHandler>();
        var handler = new GetAllEmployeesQueryHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            loggerMock.Object);

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

        var identityServiceMock = new Mock<IIdentityService>();
        identityServiceMock
            .Setup(x => x.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _cacheServiceMock.Object,
            identityServiceMock.Object,
            _createLoggerMock.Object);

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

        _createResult = await handler.Handle(command, CancellationToken.None);

        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _createResult.IsSuccess ? 201 : 400;
    }

    [When(@"o usuário atualiza o funcionário com dados válidos")]
    public async Task QuandoOUsuarioAtualizaOFuncionarioComDadosValidos()
    {
        var employeeId = _scenarioContext.Get<Guid>("EmployeeId");

        _repositoryMock
            .Setup(x => x.EmailExistsAsync(It.IsAny<string>(), employeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _stopwatch.Restart();

        var command = new UpdateEmployeeCommand(
            employeeId,
            "João",
            "Silva Atualizado",
            "joao.atualizado@supply.com",
            TestHelper.GenerateAdultBirthDate(),
            null,
            new List<string> { "11999999999" },
            null,
            _currentUserRole);

        var handler = new UpdateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _identityServiceMock.Object,
            _updateLoggerMock.Object);

        _updateResult = await handler.Handle(command, CancellationToken.None);

        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _updateResult.IsSuccess ? 200 : 400;
    }

    [When(@"o usuário busca funcionário por email ""(.*)""")]
    public async Task QuandoOUsuarioBuscaFuncionarioPorEmail(string email)
    {

        var employeeToFind = _employees.FirstOrDefault() ?? TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(employeeToFind);

        _cacheServiceMock
            .Setup(x => x.GetOrSetAsync<EmployeeResponse>(
                It.IsAny<string>(),
                It.IsAny<Func<Task<EmployeeResponse?>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, Func<Task<EmployeeResponse?>> factory, TimeSpan? expiration, CancellationToken ct)
                => factory().Result);

        _stopwatch.Restart();

        var loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetEmployeeByEmailQueryHandler>();
        var handler = new GetEmployeeByEmailQueryHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            loggerMock.Object);

        var query = new GetEmployeeByEmailQuery(email);
        _employeeResult = await handler.Handle(query, CancellationToken.None);

        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _employeeResult != null ? 200 : 404;

        _scenarioContext.Set(email, "SearchEmail");
    }

    [Given(@"que existe um funcionário cadastrado para exclusão")]
    public void DadoQueExisteUmFuncionarioCadastradoParaExclusao()
    {
        _employeeToDelete = TestHelper.CreateValidEmployee();

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employeeToDelete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToDelete);

        _repositoryMock
            .Setup(x => x.GetByIdForDeleteAsync(_employeeToDelete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToDelete);

        _repositoryMock
            .Setup(x => x.HasSubordinatesAsync(_employeeToDelete.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.SoftDeleteAsync(_employeeToDelete.Id, It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _employeeToDelete.Delete();
            })
            .Returns(Task.CompletedTask);

        _scenarioContext.Set(_employeeToDelete.Id, "EmployeeToDeleteId");
    }

    [When(@"o usuário exclui o funcionário")]
    public async Task QuandoOUsuarioExcluiOFuncionario()
    {
        var employeeId = _scenarioContext.Get<Guid>("EmployeeToDeleteId");

        _stopwatch.Restart();

        var command = new DeleteEmployeeCommand(employeeId, _currentUserRole);

        var handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            _deleteLoggerMock.Object);

        _deleteResult = await handler.Handle(command, CancellationToken.None);

        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _deleteResult.IsSuccess ? 204 : 400;
    }

    [Given(@"que existe um funcionário cadastrado com email ""(.*)"" e senha ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComEmailESenha(string email, string senha)
    {
        _loginEmail = email;
        _loginPassword = senha;

        var hashedPassword = "hashed_password_securely";

        var createResult = Employee.Create(
            "Test",
            "User",
            email,
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            hashedPassword,
            Role.Employee,
            null,
            new List<string> { TestHelper.GenerateValidPhoneNumber() });

        _employeeToFind = createResult.Value!;

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToFind);

        _passwordHasherMock
            .Setup(x => x.Verify(senha, hashedPassword))
            .Returns(true);

        _scenarioContext.Set(email, "LoginEmail");
        _scenarioContext.Set(senha, "LoginPassword");
    }

    [When(@"o usuário realiza login com credenciais válidas")]
    public async Task QuandoOUsuarioRealizaLoginComCredenciaisValidas()
    {
        var email = _scenarioContext.Get<string>("LoginEmail");
        var password = _scenarioContext.Get<string>("LoginPassword");

        _stopwatch.Restart();

        var command = new LoginCommand(email, password);
        var handler = new LoginCommandHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _loginLoggerMock.Object);

        _loginResult = await handler.Handle(command, CancellationToken.None);

        _stopwatch.Stop();
        _responseTimeMs = _stopwatch.ElapsedMilliseconds;
        _httpStatus = _loginResult.IsSuccess ? 200 : 401;
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o tempo de resposta deve ser menor que (.*)ms")]
    public void EntaoOTempoDeRespostaDeveSerMenorQue(int maxMs)
    {
        _responseTimeMs.Should().BeLessThan(maxMs,
            $"O tempo de resposta foi {_responseTimeMs}ms, esperava-se menos que {maxMs}ms");
    }

    [Then(@"o sistema deve retornar apenas (.*) funcionários")]
    public void EntaoOSistemaDeveRetornarApenasFuncionarios(int quantidade)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().HaveCount(quantidade);
    }

    [Then(@"o sistema deve carregar apenas os (.*) registros solicitados")]
    public void EntaoOSistemaDeveCarregarApenasOsRegistrosSolicitados(int quantidade)
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

    [Then(@"a busca deve utilizar índice no campo email")]
    public void EntaoABuscaDeveUtilizarIndiceNoCampoEmail()
    {

        var email = _scenarioContext.Get<string>("SearchEmail");
        _repositoryMock.Verify(
            x => x.GetByEmailAsync(email, It.IsAny<CancellationToken>()),
            Times.Once,
            "A busca por email deve utilizar o método GetByEmailAsync que usa índice no banco de dados");
    }

    [Then(@"o uso de memória deve permanecer estável")]
    public void EntaoOUsoDeMemoriaDevePermanecerEstavel()
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().NotBeNull();

        var pageSize = _pagedResult.Items.Count();
        pageSize.Should().BeLessThanOrEqualTo(100,
            "A paginação deve limitar o número de registros carregados em memória");

        if (_pagedResult.TotalCount > pageSize)
        {
            _pagedResult.Items.Count().Should().BeLessThan(_pagedResult.TotalCount,
                "O sistema deve carregar apenas uma página de registros, não todos");
        }
    }

    [Then(@"a verificação de senha deve usar algoritmo seguro")]
    public void EntaoAVerificacaoDeSenhaDeveUsarAlgoritmoSeguro()
    {
        _passwordHasherMock.Verify(
            x => x.Verify(It.IsAny<string>(), It.IsAny<string>()),
            Times.Once,
            "A verificação de senha deve usar o algoritmo seguro do PasswordHasher");
    }

    private List<(int StatusCode, long ResponseTimeMs)> _concurrentResults = new();

    [When(@"(.*) usuários fazem requisições simultâneas de listagem")]
    public async Task QuandoUsuariosFazemRequisicoesSimultaneasDeListagem(int quantidade)
    {
        _concurrentResults.Clear();
        var tasks = new List<Task>();

        for (int i = 0; i < quantidade; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    var loggerMock = Fixtures.MockFactory.CreateLoggerMock<GetAllEmployeesQueryHandler>();
                    var handler = new GetAllEmployeesQueryHandler(
                        _repositoryMock.Object,
                        _cacheServiceMock.Object,
                        loggerMock.Object);

                    var query = new GetAllEmployeesQuery(1, 10);
                    var result = await handler.Handle(query, CancellationToken.None);

                    stopwatch.Stop();

                    lock (_concurrentResults)
                    {
                        _concurrentResults.Add((200, stopwatch.ElapsedMilliseconds));
                    }
                }
                catch
                {
                    stopwatch.Stop();
                    lock (_concurrentResults)
                    {
                        _concurrentResults.Add((500, stopwatch.ElapsedMilliseconds));
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Then(@"todas as requisições devem retornar status (.*)")]
    public void EntaoTodasAsRequisicoesDevemRetornarStatus(int status)
    {
        _concurrentResults.Should().NotBeEmpty();
        _concurrentResults.Should().OnlyContain(r => r.StatusCode == status,
            $"Todas as requisições devem retornar status {status}");
    }

    [Then(@"nenhuma requisição deve exceder (.*) segundos")]
    public void EntaoNenhumaRequisicaoDeveExcederSegundos(int maxSegundos)
    {
        var maxMs = maxSegundos * 1000;
        _concurrentResults.Should().NotBeEmpty();
        _concurrentResults.Should().OnlyContain(r => r.ResponseTimeMs <= maxMs,
            $"Nenhuma requisição deve exceder {maxSegundos} segundos. " +
            $"Tempo máximo observado: {_concurrentResults.Max(r => r.ResponseTimeMs)}ms");
    }
}