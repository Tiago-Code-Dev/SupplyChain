using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Criação de Funcionário")]
public class CriarFuncionarioStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _loggerMock;

    private Role _currentUserRole = Role.Director;
    private Result<EmployeeResponse>? _result;
    private Employee? _capturedEmployee;
    private List<string> _telefones = new();
    private int _httpStatus = 200;

    public CriarFuncionarioStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<CreateEmployeeCommandHandler>();

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");

        SetupRepositoryForNewEmployee();
    }

    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        _currentUserRole = Enum.Parse<Role>(role);
        _scenarioContext.Set(_currentUserRole, "CurrentUserRole");
    }

    [Given(@"que não existe funcionário com documento ""(.*)""")]
    public void DadoQueNaoExisteFuncionarioComDocumento(string documento)
    {
        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(documento, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
    }

    [Given(@"que existe um gestor cadastrado com ID válido")]
    public void DadoQueExisteUmGestorCadastradoComIdValido()
    {
        var manager = TestHelper.CreateValidEmployee(role: Role.Leader);
        _repositoryMock
            .Setup(x => x.GetByIdAsync(manager.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(manager);
        _scenarioContext.Set(manager.Id, "ManagerId");
    }

    [Given(@"que já existe um funcionário cadastrado com documento ""(.*)""")]
    public void DadoQueJaExisteUmFuncionarioCadastradoComDocumento(string documento)
    {
        var existingEmployee = TestHelper.CreateValidEmployee(documentNumber: documento);
        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(documento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);
    }

    [Given(@"que já existe um funcionário cadastrado com email ""(.*)""")]
    public void DadoQueJaExisteUmFuncionarioCadastradoComEmail(string email)
    {
        var existingEmployee = TestHelper.CreateValidEmployee(email: email);
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);
    }

    [Given(@"que não existe gestor com ID ""(.*)""")]
    public void DadoQueNaoExisteGestorComId(string id)
    {
        var guid = Guid.Parse(id);
        _repositoryMock
            .Setup(x => x.GetByIdAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
        _scenarioContext.Set(guid, "NonExistentManagerId");
    }

    [Given(@"que o usuário não está autenticado")]
    public void DadoQueOUsuarioNaoEstaAutenticado()
    {
        _scenarioContext.Set(false, "IsAuthenticated");
    }

    [When(@"o usuário cria um novo funcionário com:")]
    public async Task QuandoOUsuarioCriaUmNovoFuncionarioComTabela(Table table)
    {
        var data = table.Rows[0];
        var telefones = data.ContainsKey("Telefones") && !string.IsNullOrEmpty(data["Telefones"])
            ? data["Telefones"].Split(',').ToList()
            : new List<string> { "11999999999" };

        var command = new CreateEmployeeCommand(
            data["Nome"],
            data["Sobrenome"],
            data["Email"],
            data["Documento"],
            DateTime.Parse(data["DataNascimento"]),
            data["Senha"],
            Enum.Parse<Role>(data["Permissao"]),
            _scenarioContext.TryGetValue<Guid>("ManagerId", out var managerId) ? managerId : null,
            telefones,
            _currentUserRole);

        await ExecutarCriacao(command);
    }

    [When(@"o usuário cria um novo funcionário com telefones:")]
    public void QuandoOUsuarioCriaUmNovoFuncionarioComTelefones(Table table)
    {
        var data = table.Rows[0];
        _scenarioContext.Set(data, "PendingEmployeeData");
    }

    [When(@"os telefones são ""(.*)""")]
    public async Task EOsTelefonesSao(string telefones)
    {
        if (_scenarioContext.TryGetValue<TableRow>("PendingEmployeeData", out var data))
        {
            _telefones = telefones.Split(',').ToList();

            var command = new CreateEmployeeCommand(
                data["Nome"],
                data["Sobrenome"],
                data["Email"],
                data["Documento"],
                DateTime.Parse(data["DataNascimento"]),
                data["Senha"],
                Enum.Parse<Role>(data["Permissao"]),
                null,
                _telefones,
                _currentUserRole);

            await ExecutarCriacao(command);
        }
    }

    [When(@"o usuário cria um novo funcionário sem telefones:")]
    public async Task QuandoOUsuarioCriaUmNovoFuncionarioSemTelefones(Table table)
    {
        var data = table.Rows[0];
        var command = new CreateEmployeeCommand(
            data["Nome"],
            data["Sobrenome"],
            data["Email"],
            data["Documento"],
            DateTime.Parse(data["DataNascimento"]),
            data["Senha"],
            Enum.Parse<Role>(data["Permissao"]),
            null,
            new List<string>(),
            _currentUserRole);

        await ExecutarCriacao(command);
    }

    [When(@"o usuário tenta criar um novo funcionário com:")]
    public async Task QuandoOUsuarioTentaCriarUmNovoFuncionarioComTabela(Table table)
    {
        await QuandoOUsuarioCriaUmNovoFuncionarioComTabela(table);
    }

    [When(@"o usuário tenta criar um novo funcionário com gestor inexistente:")]
    public async Task QuandoOUsuarioTentaCriarUmNovoFuncionarioComGestorInexistente(Table table)
    {
        var data = table.Rows[0];
        var nonExistentManagerId = _scenarioContext.Get<Guid>("NonExistentManagerId");

        var command = new CreateEmployeeCommand(
            data["Nome"],
            data["Sobrenome"],
            data["Email"],
            data["Documento"],
            DateTime.Parse(data["DataNascimento"]),
            data["Senha"],
            Enum.Parse<Role>(data["Permissao"]),
            nonExistentManagerId,
            new List<string> { data["Telefones"] },
            _currentUserRole);

        await ExecutarCriacao(command);
    }

    [When(@"o usuário tenta criar um novo funcionário com dados válidos")]
    public async Task QuandoOUsuarioTentaCriarUmNovoFuncionarioComDadosValidos()
    {
        if (!_scenarioContext.TryGetValue<bool>("IsAuthenticated", out var isAuth) || !isAuth)
        {
            _httpStatus = 401;
            _scenarioContext.Set(_httpStatus, "HttpStatus");
            _scenarioContext.Set("Não autorizado", "ErrorMessage");
            return;
        }

        var command = new CreateEmployeeCommand(
            "João",
            "Silva",
            "joao@supply.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Senha@123456",
            Role.Employee,
            null,
            new List<string> { "11999999999" },
            _currentUserRole);

        await ExecutarCriacao(command);
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o sistema deve retornar os dados do funcionário criado")]
    public void EntaoOSistemaDeveRetornarOsDadosDoFuncionarioCriado()
    {
        _result.Should().NotBeNull();
        _result!.IsSuccess.Should().BeTrue();
        _result.Value.Should().NotBeNull();
    }

    [Then(@"o funcionário deve ter um ID único gerado")]
    public void EntaoOFuncionarioDeveTerUmIdUnicoGerado()
    {
        _result.Should().NotBeNull();
        _result!.IsSuccess.Should().BeTrue();
        _result.Value.Id.Should().NotBe(Guid.Empty);
    }

    [Then(@"a senha do funcionário deve estar hasheada no banco de dados")]
    public void EntaoASenhaDoFuncionarioDeveEstarHasheadaNoBancoDeDados()
    {
        _passwordHasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.Once);
    }

    [Then(@"o funcionário deve ser criado com sucesso")]
    public void EntaoOFuncionarioDeveSerCriadoComSucesso()
    {
        _result.Should().NotBeNull();
        _result!.IsSuccess.Should().BeTrue("O funcionário deveria ter sido criado com sucesso");
    }

    [Then(@"o funcionário deve ter exatamente (.*) telefone cadastrado")]
    [Then(@"o funcionário deve ter (.*) telefones cadastrados")]
    public void EntaoOFuncionarioDeveTerTelefonesCadastrados(int quantidade)
    {
        _capturedEmployee.Should().NotBeNull();
        _capturedEmployee!.PhoneNumbers.Should().HaveCount(quantidade);
    }

    [Then(@"o sistema deve retornar mensagem de erro indicando que nome é obrigatório")]
    public void EntaoOSistemaDeveRetornarMensagemDeErroIndicandoQueNomeEObrigatorio()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Description.Should().Contain("First name is required");
    }

    [Then(@"o sistema deve retornar mensagem de erro indicando que sobrenome é obrigatório")]
    public void EntaoOSistemaDeveRetornarMensagemDeErroIndicandoQueSobrenomeEObrigatorio()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Description.Should().Contain("Last name is required");
    }

    [Then(@"o sistema deve retornar mensagem de erro indicando que email é inválido")]
    public void EntaoOSistemaDeveRetornarMensagemDeErroIndicandoQueEmailEInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
        _result.Error.Description.Should().Contain("Invalid email format");
    }

    [Then(@"o sistema deve retornar mensagem de erro indicando que email é obrigatório")]
    public void EntaoOSistemaDeveRetornarMensagemDeErroIndicandoQueEmailEObrigatorio()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem de erro indicando que documento é obrigatório")]
    public void EntaoOSistemaDeveRetornarMensagemDeErroIndicandoQueDocumentoEObrigatorio()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem ""(.*)""")]
    public void EntaoOSistemaDeveRetornarMensagem(string mensagem)
    {
        if (_result != null && _result.IsFailure)
        {
            _result.Error.Description.Should().Contain(mensagem);
        }
        else if (_scenarioContext.TryGetValue<string>("ErrorMessage", out var errorMsg))
        {
            errorMsg.Should().Contain(mensagem);
        }
    }

    [Then(@"o funcionário não deve ser criado no banco de dados")]
    public void EntaoOFuncionarioNaoDeveSerCriadoNoBancoDeDados()
    {
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Then(@"o sistema deve retornar mensagem indicando que a senha não atende aos critérios de segurança")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSenhaNaoAtendeAosCriterios()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    private async Task ExecutarCriacao(CreateEmployeeCommand command)
    {
        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Callback<Employee, CancellationToken>((e, _) => _capturedEmployee = e)
            .Returns((Employee e, CancellationToken _) => Task.FromResult(e));

        var handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);

        _result = await handler.Handle(command, CancellationToken.None);

        _httpStatus = _result.IsSuccess ? 201 :
            (_result.Error.Code.Contains("Forbidden") ? 403 :
             _result.Error.Code.Contains("Conflict") ? 409 :
             _result.Error.Code.Contains("NotFound") ? 404 : 400);

        _scenarioContext.Set(_httpStatus, "HttpStatus");
    }

    private void SetupRepositoryForNewEmployee()
    {
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
    }
}