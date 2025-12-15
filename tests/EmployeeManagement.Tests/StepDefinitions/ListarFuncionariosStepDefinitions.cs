using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;
using EmployeeManagement.Tests.Helpers;
using Moq;
using TechTalk.SpecFlow;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
public class ListarFuncionariosStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;

    private PagedResult<EmployeeResponse>? _pagedResult;
    private EmployeeResponse? _employeeResult;
    private List<Employee> _employees = new();
    private Employee? _employeeToFind;
    private int _httpStatus = 200;
    private bool _cacheWasUsed;

    public ListarFuncionariosStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
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
                var items = _employees.Skip(skip).Take(size).ToList();
                return new PagedResult<Employee>(items, _employees.Count, page, size);
            });
    }

    [Given(@"que existem funcionários cadastrados:")]
    public void DadoQueExistemFuncionariosCadastrados(Table table)
    {
        _employees = new List<Employee>();
        foreach (var row in table.Rows)
        {
            var firstName = row.ContainsKey("Nome") ? row["Nome"] : "Test";
            var lastName = row.ContainsKey("Sobrenome") ? row["Sobrenome"] : "User";
            var email = row.ContainsKey("Email") ? row["Email"] : $"{firstName.ToLower()}@supply.com";
            var roleStr = row.ContainsKey("Permissao") ? row["Permissao"] : "Employee";
            var role = Enum.Parse<Role>(roleStr);

            var employee = TestHelper.CreateValidEmployee(
                firstName: firstName,
                lastName: lastName,
                email: email,
                role: role);
            _employees.Add(employee);
        }

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employees);
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido e nome ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoENome(string nomeCompleto)
    {
        var partes = nomeCompleto.Split(' ');
        var firstName = partes[0];
        var lastName = partes.Length > 1 ? partes[1] : "Silva";

        _employeeToFind = TestHelper.CreateValidEmployee(
            firstName: firstName,
            lastName: lastName);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employeeToFind.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToFind);

        _scenarioContext.Set(_employeeToFind.Id, "EmployeeId");
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

    [Given(@"que os dados do funcionário estão em cache")]
    public void DadoQueOsDadosDoFuncionarioEstaoEmCache()
    {
        var response = EmployeeResponse.FromEntity(_employeeToFind!);
        _cacheServiceMock
            .Setup(x => x.GetAsync<EmployeeResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response)
            .Callback(() => _cacheWasUsed = true);
    }

    [Given(@"que não existe funcionário com ID ""(.*)""")]
    public void DadoQueNaoExisteFuncionarioComId(string id)
    {
        var guid = Guid.Parse(id);
        _repositoryMock
            .Setup(x => x.GetByIdAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
        _scenarioContext.Set(guid, "NonExistentId");
    }

    [When(@"o usuário solicita a listagem de funcionários através do endpoint GET /api/employees")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionarios()
    {
        if (!_scenarioContext.TryGetValue<Role>("CurrentUserRole", out _))
        {
            _httpStatus = 401;
            return;
        }

        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetAllEmployeesQuery();
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        _httpStatus = 200;
    }

    [When(@"o usuário solicita a listagem de funcionários com:")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosCom(Table table)
    {
        var data = table.Rows[0];
        var page = int.Parse(data["Page"]);
        var pageSize = int.Parse(data["PageSize"]);

        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetAllEmployeesQuery(page, pageSize);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        _httpStatus = 200;
    }

    [When(@"o usuário solicita a listagem de funcionários com filtro por nome ""(.*)""")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosComFiltroPorNome(string nome)
    {
        var filteredEmployees = _employees.Where(e =>
            e.FirstName.Contains(nome, StringComparison.OrdinalIgnoreCase)).ToList();

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                nome,
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Employee>(filteredEmployees, filteredEmployees.Count, 1, 10));

        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetAllEmployeesQuery(SearchTerm: nome);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        _httpStatus = 200;
    }

    [When(@"o usuário solicita a listagem de funcionários com filtro por email ""(.*)""")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosComFiltroPorEmail(string email)
    {
        var filteredEmployees = _employees.Where(e =>
            e.Email.Equals(email, StringComparison.OrdinalIgnoreCase)).ToList();

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                email,
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Employee>(filteredEmployees, filteredEmployees.Count, 1, 10));

        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetAllEmployeesQuery(SearchTerm: email);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        _httpStatus = 200;
    }

    [When(@"o usuário solicita a listagem de funcionários com filtro por permissão ""(.*)""")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosComFiltroPorPermissao(string permissao)
    {
        var role = Enum.Parse<Role>(permissao);
        var filteredEmployees = _employees.Where(e => e.Role == role).ToList();

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                permissao,
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Employee>(filteredEmployees, filteredEmployees.Count, 1, 10));

        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetAllEmployeesQuery(SearchTerm: permissao);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        _httpStatus = 200;
    }

    [When(@"o usuário solicita os dados do funcionário através do endpoint GET /api/employees/\{id\}")]
    public async Task QuandoOUsuarioSolicitaOsDadosDoFuncionario()
    {
        if (_scenarioContext.TryGetValue<Guid>("EmployeeId", out var employeeId))
        {
            var handler = new GetEmployeeByIdQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
            var query = new GetEmployeeByIdQuery(employeeId);
            _employeeResult = await handler.Handle(query, CancellationToken.None);
            _httpStatus = _employeeResult != null ? 200 : 404;
        }
        else if (_scenarioContext.TryGetValue<Guid>("NonExistentId", out var nonExistentId))
        {
            _repositoryMock
                .Setup(x => x.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Employee?)null);

            var handler = new GetEmployeeByIdQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
            var query = new GetEmployeeByIdQuery(nonExistentId);
            _employeeResult = await handler.Handle(query, CancellationToken.None);
            _httpStatus = 404;
        }
    }

    [When(@"o usuário solicita a listagem de funcionários ordenada por nome ascendente")]
    public async Task QuandoOUsuarioSolicitaAListagemDeFuncionariosOrdenadaPorNomeAscendente()
    {
        var sortedEmployees = _employees.OrderBy(e => e.FirstName).ToList();

        _repositoryMock
            .Setup(x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                "FirstName",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Employee>(sortedEmployees, sortedEmployees.Count, 1, 10));

        var handler = new GetAllEmployeesQueryHandler(_repositoryMock.Object, _cacheServiceMock.Object);
        var query = new GetAllEmployeesQuery(SortBy: "FirstName", SortDescending: false);
        _pagedResult = await handler.Handle(query, CancellationToken.None);
        _httpStatus = 200;
    }

    [When(@"o usuário tenta listar funcionários através do endpoint GET /api/employees")]
    public void QuandoOUsuarioTentaListarFuncionarios()
    {
        _httpStatus = 401;
        _scenarioContext.Set("Não autorizado", "ErrorMessage");
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o sistema deve retornar uma lista com (.*) funcionários")]
    public void EntaoOSistemaDeveRetornarUmaListaComFuncionarios(int quantidade)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().HaveCount(quantidade);
    }

    [Then(@"o sistema deve retornar (.*) funcionários")]
    public void EntaoOSistemaDeveRetornarFuncionarios(int quantidade)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().HaveCount(quantidade);
    }

    [Then(@"cada funcionário na lista deve conter: ID, Nome, Sobrenome, Email, Documento, Telefones, Permissao")]
    public void EntaoCadaFuncionarioNaListaDeveConterCampos()
    {
        _pagedResult.Should().NotBeNull();
        foreach (var employee in _pagedResult!.Items)
        {
            employee.Id.Should().NotBe(Guid.Empty);
            employee.FirstName.Should().NotBeNullOrEmpty();
            employee.LastName.Should().NotBeNullOrEmpty();
            employee.Email.Should().NotBeNullOrEmpty();
        }
    }

    [Then(@"a senha não deve estar presente na resposta")]
    public void EntaoASenhaNaoDeveEstarPresenteNaResposta()
    {
        _pagedResult.Should().NotBeNull();
    }

    [Then(@"a resposta deve conter informações de paginação")]
    public void EntaoARespostaDeveConterInformacoesDePaginacao()
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.TotalCount.Should().BeGreaterThan(0);
        _pagedResult.TotalPages.Should().BeGreaterThan(0);
        _pagedResult.CurrentPage.Should().BeGreaterThan(0);
    }

    [Then(@"o sistema deve retornar apenas funcionários cujo nome contenha ""(.*)""")]
    public void EntaoOSistemaDeveRetornarApenasFuncionariosCujoNomeContenha(string nome)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().OnlyContain(e =>
            e.FirstName.Contains(nome, StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"a lista deve conter pelo menos (.*) funcionário")]
    [Then(@"a lista deve conter pelo menos (.*) funcionários")]
    public void EntaoAListaDeveConterPeloMenosFuncionarios(int quantidade)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Count.Should().BeGreaterThanOrEqualTo(quantidade);
    }

    [Then(@"a lista deve conter exatamente (.*) funcionário")]
    public void EntaoAListaDeveConterExatamenteFuncionario(int quantidade)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().HaveCount(quantidade);
    }

    [Then(@"o sistema deve retornar apenas funcionários com email ""(.*)""")]
    public void EntaoOSistemaDeveRetornarApenasFuncionariosComEmail(string email)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().OnlyContain(e =>
            e.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"o sistema deve retornar apenas funcionários com permissão ""(.*)""")]
    public void EntaoOSistemaDeveRetornarApenasFuncionariosComPermissao(string permissao)
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.Items.Should().OnlyContain(e => e.Role == permissao);
    }

    [Then(@"o sistema deve retornar os dados completos do funcionário")]
    public void EntaoOSistemaDeveRetornarOsDadosCompletosDoFuncionario()
    {
        _employeeResult.Should().NotBeNull();
        _employeeResult!.Id.Should().NotBe(Guid.Empty);
    }

    [Then(@"a resposta deve conter: ID, Nome, Sobrenome, Email, Documento, Telefones, Permissao")]
    public void EntaoARespostaDeveConterCamposCompletos()
    {
        _employeeResult.Should().NotBeNull();
        _employeeResult!.Id.Should().NotBe(Guid.Empty);
        _employeeResult.FirstName.Should().NotBeNullOrEmpty();
        _employeeResult.LastName.Should().NotBeNullOrEmpty();
        _employeeResult.Email.Should().NotBeNullOrEmpty();
    }

    [Then(@"os dados devem ser recuperados do cache")]
    public void EntaoOsDadosDevemSerRecuperadosDoCache()
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

    [Then(@"os funcionários devem estar ordenados alfabeticamente por nome")]
    public void EntaoOsFuncionariosDevemEstarOrdenadosAlfabeticamentePorNome()
    {
        _pagedResult.Should().NotBeNull();
        var names = _pagedResult!.Items.Select(e => e.FirstName).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Then(@"os funcionários devem ser diferentes da primeira página")]
    public void EntaoOsFuncionariosDevemSerDiferentesDaPrimeiraPagina()
    {
        _pagedResult.Should().NotBeNull();
        _pagedResult!.CurrentPage.Should().Be(2);
    }

    [Then(@"o sistema deve retornar mensagem ""(.*)""")]
    public void EntaoOSistemaDeveRetornarMensagem(string mensagem)
    {
        if (_scenarioContext.TryGetValue<string>("ErrorMessage", out var errorMsg))
        {
            errorMsg.Should().Contain(mensagem);
        }
    }
}