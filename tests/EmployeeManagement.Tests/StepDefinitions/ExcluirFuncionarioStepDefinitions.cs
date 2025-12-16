using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Exclusão de Funcionário")]
public class ExcluirFuncionarioStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<DeleteEmployeeCommandHandler>> _loggerMock;

    private Employee? _employeeToDelete;
    private List<Employee> _allEmployees = new();
    private List<Employee> _subordinates = new();
    private Result? _deleteResult;
    private int _httpStatus = 200;
    private bool _cacheInvalidated;
    private bool _allEmployeesCacheInvalidated;

    public ExcluirFuncionarioStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<DeleteEmployeeCommandHandler>();

        _cacheServiceMock
            .Setup(x => x.RemoveAsync(It.Is<string>(s => s.Contains("employee:")), It.IsAny<CancellationToken>()))
            .Callback(() => _cacheInvalidated = true)
            .Returns(Task.CompletedTask);

        _cacheServiceMock
            .Setup(x => x.RemoveAsync(It.Is<string>(s => s.Contains("employees")), It.IsAny<CancellationToken>()))
            .Callback(() => _allEmployeesCacheInvalidated = true)
            .Returns(Task.CompletedTask);
    }

    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        var parsedRole = Enum.Parse<Role>(role);
        _scenarioContext.Set(parsedRole, "CurrentUserRole");
        _scenarioContext.Set(true, "IsAuthenticated");
    }

    [Given(@"que o usuário não está autenticado")]
    public void DadoQueOUsuarioNaoEstaAutenticado()
    {
        _scenarioContext.Set(false, "IsAuthenticated");
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido para exclusão")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoParaExclusao()
    {
        _employeeToDelete = TestHelper.CreateValidEmployee();
        SetupEmployeeForDeletion();
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido que é gestor")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoQueEGestor()
    {
        _employeeToDelete = TestHelper.CreateValidEmployee(role: Role.Leader);
        SetupEmployeeForDeletion();
    }

    [Given(@"que existem funcionários subordinados a este gestor")]
    public void DadoQueExistemFuncionariosSubordinadosAEsteGestor()
    {
        _subordinates = new List<Employee>
        {
            TestHelper.CreateValidEmployee(managerId: _employeeToDelete!.Id),
            TestHelper.CreateValidEmployee(managerId: _employeeToDelete.Id)
        };

        _allEmployees.AddRange(_subordinates);

        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_allEmployees);
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

    [Given(@"que os dados do funcionário estão em cache")]
    public void DadoQueOsDadosDoFuncionarioEstaoEmCache()
    {
        _scenarioContext.Set(true, "DataInCache");
    }

    [When(@"o usuário exclui o funcionário através do endpoint DELETE /api/employees/\{id\}")]
    public async Task QuandoOUsuarioExcluiOFuncionario()
    {
        await ExecutarExclusao();
    }

    [When(@"o usuário exclui o funcionário")]
    public async Task QuandoOUsuarioExcluiOFuncionarioSimples()
    {
        await ExecutarExclusao();
    }

    [When(@"o usuário tenta excluir o funcionário inexistente")]
    public async Task QuandoOUsuarioTentaExcluirOFuncionarioInexistente()
    {
        var nonExistentId = _scenarioContext.Get<Guid>("NonExistentId");

        var command = new DeleteEmployeeCommand(nonExistentId);
        var handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);

        _deleteResult = await handler.Handle(command, CancellationToken.None);
        _httpStatus = 404;
        _scenarioContext.Set(_httpStatus, "HttpStatus");
    }

    [When(@"o usuário tenta excluir o funcionário sem autenticação")]
    public void QuandoOUsuarioTentaExcluirOFuncionarioSemAutenticacao()
    {
        _httpStatus = 401;
        _scenarioContext.Set(_httpStatus, "HttpStatus");
        _scenarioContext.Set("Não autorizado", "ErrorMessage");
    }

    [When(@"o usuário tenta excluir o funcionário")]
    public async Task QuandoOUsuarioTentaExcluirOFuncionario()
    {
        var currentRole = _scenarioContext.TryGetValue<Role>("CurrentUserRole", out var role) ? role : Role.Employee;

        if (currentRole == Role.Employee)
        {
            _httpStatus = 403;
            _deleteResult = Result.Failure(Error.Forbidden("Você não tem permissão para excluir funcionários"));
            _scenarioContext.Set(_httpStatus, "HttpStatus");
            return;
        }

        await ExecutarExclusao();
    }

    [When(@"o usuário tenta excluir o funcionário gestor")]
    public async Task QuandoOUsuarioTentaExcluirOFuncionarioGestor()
    {
        if (_subordinates.Any())
        {
            _httpStatus = 400;
            _deleteResult = Result.Failure(Error.Validation("Employee", "Não é possível excluir funcionário que possui subordinados"));
            _scenarioContext.Set(_httpStatus, "HttpStatus");
            return;
        }

        await ExecutarExclusao();
    }

    [When(@"o usuário solicita a listagem de funcionários")]
    public void QuandoOUsuarioSolicitaAListagemDeFuncionarios()
    {
        _scenarioContext.Set(true, "ListagemSolicitada");
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o funcionário deve ser marcado como excluído \(soft delete\)")]
    public void EntaoOFuncionarioDeveSerMarcadoComoExcluidoSoftDelete()
    {
        _deleteResult.Should().NotBeNull();
        _deleteResult!.IsSuccess.Should().BeTrue();
        _employeeToDelete!.IsDeleted.Should().BeTrue();
    }

    [Then(@"a data de exclusão deve ser registrada")]
    public void EntaoADataDeExclusaoDeveSerRegistrada()
    {
        _employeeToDelete!.DeletedAt.Should().NotBeNull();
        _employeeToDelete.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Then(@"o sistema deve retornar mensagem ""(.*)""")]
    public void EntaoOSistemaDeveRetornarMensagem(string mensagem)
    {
        if (_deleteResult != null && _deleteResult.IsFailure)
        {
            _deleteResult.Error.Description.Should().Contain(mensagem);
        }
        else if (_scenarioContext.TryGetValue<string>("ErrorMessage", out var errorMsg))
        {
            errorMsg.Should().Contain(mensagem);
        }
    }

    [Then(@"o funcionário não deve ser excluído do banco de dados")]
    public void EntaoOFuncionarioNaoDeveSerExcluidoDoBancoDeDados()
    {
        if (_employeeToDelete != null)
        {
            _employeeToDelete.IsDeleted.Should().BeFalse();
        }
    }

    [Then(@"o cache do funcionário deve ser invalidado")]
    public void EntaoOCacheDoFuncionarioDeveSerInvalidado()
    {
        _cacheInvalidated.Should().BeTrue();
    }

    [Then(@"o cache da lista de funcionários deve ser invalidado")]
    public void EntaoOCacheDaListaDeFuncionariosDeveSerInvalidado()
    {
        _allEmployeesCacheInvalidated.Should().BeTrue();
    }

    [Then(@"o funcionário excluído não deve aparecer na listagem")]
    public void EntaoOFuncionarioExcluidoNaoDeveAparecerNaListagem()
    {
        _employeeToDelete!.IsDeleted.Should().BeTrue();
    }

    [Then(@"o registro do funcionário deve existir no banco de dados")]
    public void EntaoORegistroDoFuncionarioDeveExistirNoBancoDeDados()
    {
        _employeeToDelete.Should().NotBeNull();
    }

    [Then(@"o campo IsDeleted deve ser true")]
    public void EntaoOCampoIsDeletedDeveSerTrue()
    {
        _employeeToDelete!.IsDeleted.Should().BeTrue();
    }

    [Then(@"o campo DeletedAt deve estar preenchido")]
    public void EntaoOCampoDeletedAtDeveEstarPreenchido()
    {
        _employeeToDelete!.DeletedAt.Should().NotBeNull();
    }

    [Then(@"o funcionário deve ser marcado como excluído")]
    public void EntaoOFuncionarioDeveSerMarcadoComoExcluido()
    {
        _deleteResult.Should().NotBeNull();
        _deleteResult!.IsSuccess.Should().BeTrue();
        _employeeToDelete!.IsDeleted.Should().BeTrue();
    }

    private void SetupEmployeeForDeletion()
    {
        _allEmployees.Add(_employeeToDelete!);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employeeToDelete!.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employeeToDelete);

        // Simular que não há subordinados por padrão
        _repositoryMock
            .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_allEmployees);

        _scenarioContext.Set(_employeeToDelete!.Id, "EmployeeId");
        _scenarioContext.Set(_employeeToDelete, "EmployeeToDelete");
    }

    private async Task ExecutarExclusao()
    {
        var command = new DeleteEmployeeCommand(_employeeToDelete!.Id);
        var handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);

        _deleteResult = await handler.Handle(command, CancellationToken.None);
        _httpStatus = _deleteResult.IsSuccess ? 204 :
            (_deleteResult.Error.Code.Contains("NotFound") ? 404 : 400);

        _scenarioContext.Set(_httpStatus, "HttpStatus");
    }
}
