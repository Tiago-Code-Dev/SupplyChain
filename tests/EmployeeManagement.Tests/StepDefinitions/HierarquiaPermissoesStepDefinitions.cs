using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Hierarquia de Permissões")]
public class HierarquiaPermissoesStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _createLoggerMock;
    private readonly Mock<ILogger<UpdateEmployeeCommandHandler>> _updateLoggerMock;
    private readonly Mock<ILogger<DeleteEmployeeCommandHandler>> _deleteLoggerMock;

    private Role _currentUserRole = Role.Employee;
    private Result<EmployeeResponse>? _createResult;
    private Result<EmployeeResponse>? _updateResult;
    private Result? _deleteResult;
    private Employee? _targetEmployee;
    private int _httpStatus = 200;

    public HierarquiaPermissoesStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
        _createLoggerMock = Fixtures.MockFactory.CreateLoggerMock<CreateEmployeeCommandHandler>();
        _updateLoggerMock = Fixtures.MockFactory.CreateLoggerMock<UpdateEmployeeCommandHandler>();
        _deleteLoggerMock = Fixtures.MockFactory.CreateLoggerMock<DeleteEmployeeCommandHandler>();

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

    [Given(@"que existe um funcionário com permissão ""(.*)""")]
    public void DadoQueExisteUmFuncionarioComPermissao(string permissao)
    {
        var role = Enum.Parse<Role>(permissao);
        _targetEmployee = TestHelper.CreateValidEmployee(role: role);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_targetEmployee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_targetEmployee);

        _scenarioContext.Set(_targetEmployee, "TargetEmployee");
    }

    [Given(@"que existe um funcionário cadastrado para exclusão")]
    public void DadoQueExisteUmFuncionarioCadastradoParaExclusao()
    {
        _targetEmployee = TestHelper.CreateValidEmployee(role: Role.Employee);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_targetEmployee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_targetEmployee);

        _scenarioContext.Set(_targetEmployee, "TargetEmployee");
    }

    [Given(@"que existe um funcionário com permissão ""(.*)"" para exclusão")]
    public void DadoQueExisteUmFuncionarioComPermissaoParaExclusao(string permissao)
    {
        var role = Enum.Parse<Role>(permissao);
        _targetEmployee = TestHelper.CreateValidEmployee(role: role);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_targetEmployee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_targetEmployee);

        _scenarioContext.Set(_targetEmployee, "TargetEmployee");
    }

    [When(@"o usuário cria um novo funcionário com permissão ""(.*)""")]
    public async Task QuandoOUsuarioCriaUmNovoFuncionarioComPermissao(string permissao)
    {
        var targetRole = Enum.Parse<Role>(permissao);
        await ExecutarCriacao(targetRole);
    }

    [When(@"o usuário tenta criar um novo funcionário com permissão ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmNovoFuncionarioComPermissao(string permissao)
    {
        var targetRole = Enum.Parse<Role>(permissao);
        await ExecutarCriacao(targetRole);
    }

    [When(@"o usuário atualiza o funcionário para permissão ""(.*)""")]
    public async Task QuandoOUsuarioAtualizaOFuncionarioParaPermissao(string permissao)
    {
        var targetRole = Enum.Parse<Role>(permissao);
        await ExecutarAtualizacaoPermissao(targetRole);
    }

    [When(@"o usuário tenta atualizar o funcionário para permissão ""(.*)""")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioParaPermissao(string permissao)
    {
        var targetRole = Enum.Parse<Role>(permissao);
        await ExecutarAtualizacaoPermissao(targetRole);
    }

    [When(@"o usuário exclui o funcionário")]
    public async Task QuandoOUsuarioExcluiOFuncionario()
    {
        await ExecutarExclusao();
    }

    [When(@"o usuário tenta excluir o funcionário")]
    public async Task QuandoOUsuarioTentaExcluirOFuncionario()
    {
        await ExecutarExclusao();
    }

    [Then(@"o funcionário deve ser criado com sucesso")]
    public void EntaoOFuncionarioDeveSerCriadoComSucesso()
    {
        _createResult.Should().NotBeNull();
        _createResult!.IsSuccess.Should().BeTrue("O funcionário deveria ter sido criado com sucesso");
    }

    [Then(@"o funcionário deve ter permissão ""(.*)""")]
    public void EntaoOFuncionarioDeveTerPermissao(string permissao)
    {
        if (_createResult != null && _createResult.IsSuccess)
        {
            _createResult.Value.Role.ToString().Should().Be(permissao);
        }
        else if (_updateResult != null && _updateResult.IsSuccess)
        {
            _updateResult.Value.Role.ToString().Should().Be(permissao);
        }
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o sistema deve retornar mensagem ""(.*)""")]
    public void EntaoOSistemaDeveRetornarMensagem(string mensagem)
    {
        if (_createResult != null && _createResult.IsFailure)
        {
            _createResult.Error.Description.Should().Contain(mensagem);
        }
        else if (_updateResult != null && _updateResult.IsFailure)
        {
            _updateResult.Error.Description.Should().Contain(mensagem);
        }
        else if (_deleteResult != null && _deleteResult.IsFailure)
        {
            _deleteResult.Error.Description.Should().Contain(mensagem);
        }
    }

    [Then(@"o funcionário não deve ser criado no banco de dados")]
    public void EntaoOFuncionarioNaoDeveSerCriadoNoBancoDeDados()
    {
        _repositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Then(@"o funcionário não deve ser atualizado no banco de dados")]
    public void EntaoOFuncionarioNaoDeveSerAtualizadoNoBancoDeDados()
    {
        _repositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Then(@"o funcionário deve ser marcado como excluído")]
    public void EntaoOFuncionarioDeveSerMarcadoComoExcluido()
    {
        _deleteResult.Should().NotBeNull();
        _deleteResult!.IsSuccess.Should().BeTrue();
        _targetEmployee!.IsDeleted.Should().BeTrue();
    }

    private async Task ExecutarCriacao(Role targetRole)
    {
        var identityServiceMock = new Mock<IIdentityService>();
        identityServiceMock
            .Setup(x => x.CreateUserAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var command = new CreateEmployeeCommand(
            "Test",
            "User",
            $"test{Guid.NewGuid()}@supply.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Senha@123456",
            targetRole,
            null,
            new List<string> { "11999999999" },
            _currentUserRole);

        var handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _cacheServiceMock.Object,
            identityServiceMock.Object,
            _createLoggerMock.Object);

        _createResult = await handler.Handle(command, CancellationToken.None);

        _httpStatus = _createResult.IsSuccess ? 201 :
            (_createResult.Error.Code.Contains("Forbidden") ? 403 : 400);
    }

    private async Task ExecutarAtualizacaoPermissao(Role targetRole)
    {
        if (_currentUserRole < targetRole)
        {
            _httpStatus = 403;
            _updateResult = Result<EmployeeResponse>.Failure(
                Error.Forbidden("Você não tem permissão para alterar usuários para nível de permissão superior"));
        }
        else
        {
            var command = new UpdateEmployeeCommand(
                _targetEmployee!.Id,
                _targetEmployee.FirstName,
                _targetEmployee.LastName,
                _targetEmployee.Email,
                _targetEmployee.BirthDate,
                null,
                new List<string> { "11999999999" },
                targetRole,
                _currentUserRole);

            var handler = new UpdateEmployeeCommandHandler(
                _repositoryMock.Object,
                _unitOfWorkMock.Object,
                _cacheServiceMock.Object,
                _updateLoggerMock.Object);

            _repositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _updateResult = await handler.Handle(command, CancellationToken.None);
            _httpStatus = _updateResult.IsSuccess ? 200 : 400;
        }
    }

    private async Task ExecutarExclusao()
    {
        if (_currentUserRole == Role.Employee)
        {
            _httpStatus = 403;
            _deleteResult = Result.Failure(
                Error.Forbidden("Você não tem permissão para excluir funcionários"));
            return;
        }

        var command = new DeleteEmployeeCommand(_targetEmployee!.Id, _currentUserRole);

        var handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            _deleteLoggerMock.Object);

        _deleteResult = await handler.Handle(command, CancellationToken.None);
        _httpStatus = _deleteResult.IsSuccess ? 204 :
            (_deleteResult.Error.Code.Contains("NotFound") ? 404 : 400);
    }

    private void SetupRepositoryForNewEmployee()
    {
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee e, CancellationToken _) => e);
    }
}
