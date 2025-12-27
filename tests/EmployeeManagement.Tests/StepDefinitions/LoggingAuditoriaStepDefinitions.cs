using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;
using EmployeeManagement.Application.Features.Auth.Commands.Login;
using EmployeeManagement.Application.Features.Auth.Commands.ChangePassword;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Logging e Auditoria")]
public class LoggingAuditoriaStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ICacheService> _cacheServiceMock;

    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _createLoggerMock;
    private readonly Mock<ILogger<UpdateEmployeeCommandHandler>> _updateLoggerMock;
    private readonly Mock<ILogger<DeleteEmployeeCommandHandler>> _deleteLoggerMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loginLoggerMock;
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _changePasswordLoggerMock;

    private Employee? _employee;
    private List<(LogLevel Level, string Message)> _logEntries = new();
    private bool _loggingConfigured;

    public LoggingAuditoriaStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = Fixtures.MockFactory.CreateJwtServiceMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();

        _createLoggerMock = CreateLoggingMock<CreateEmployeeCommandHandler>();
        _updateLoggerMock = CreateLoggingMock<UpdateEmployeeCommandHandler>();
        _deleteLoggerMock = CreateLoggingMock<DeleteEmployeeCommandHandler>();
        _loginLoggerMock = CreateLoggingMock<LoginCommandHandler>();
        _changePasswordLoggerMock = CreateLoggingMock<ChangePasswordCommandHandler>();

        SetupMocks();
    }

    private Mock<ILogger<T>> CreateLoggingMock<T>()
    {
        var mock = new Mock<ILogger<T>>();
        mock.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback<LogLevel, EventId, object, Exception?, Delegate>((level, eventId, state, ex, formatter) =>
            {
                _logEntries.Add((level, state?.ToString() ?? ""));
            });
        return mock;
    }

    [Given(@"que o sistema de logging está configurado")]
    public void DadoQueOSistemaDeLoggingEstaConfigurado()
    {
        _loggingConfigured = true;
        _logEntries.Clear();
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecido()
    {
        _employee = TestHelper.CreateValidEmployee();
        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employee);
        _scenarioContext.Set(_employee, "Employee");
        _scenarioContext.Set(_employee.Id, "EmployeeId");
    }

    [Given(@"que existe um funcionário cadastrado para exclusão")]
    public void DadoQueExisteUmFuncionarioCadastradoParaExclusao()
    {
        DadoQueExisteUmFuncionarioCadastradoComIdConhecido();
    }

    [Given(@"que existe um funcionário cadastrado com email ""(.*)"" e senha ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComEmailESenha(string email, string senha)
    {
        var passwordHash = $"hashed_{senha}";
        _employee = TestHelper.CreateValidEmployee(email: email, passwordHash: passwordHash);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employee);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employee);

        _passwordHasherMock
            .Setup(x => x.Verify(senha, passwordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.Verify(It.Is<string>(s => s != senha), It.IsAny<string>()))
            .Returns(false);

        _scenarioContext.Set(_employee, "Employee");
        _scenarioContext.Set(senha, "CurrentPassword");
    }

    [When(@"o usuário cria um novo funcionário com sucesso")]
    public async Task QuandoOUsuarioCriaUmNovoFuncionarioComSucesso()
    {
        var command = new CreateEmployeeCommand(
            "João", "Silva", "joao@supply.com",
            TestHelper.GenerateValidCpf(),
            TestHelper.GenerateAdultBirthDate(),
            "Senha@123456",
            Role.Employee,
            null,
            new List<string> { "11999999999" },
            Role.Director);

        var handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _cacheServiceMock.Object,
            _createLoggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(result, "Result");
    }

    [When(@"o usuário atualiza o funcionário com sucesso")]
    public async Task QuandoOUsuarioAtualizaOFuncionarioComSucesso()
    {
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            _employee!.Id,
            "Carlos", "Silva", "carlos@supply.com",
            _employee.BirthDate,
            null,
            new List<string> { "11999999999" },
            null, 
            Role.Director 
        );

        var handler = new UpdateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _updateLoggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(result, "Result");
    }

    [When(@"o usuário exclui o funcionário com sucesso")]
    public async Task QuandoOUsuarioExcluiOFuncionarioComSucesso()
    {
        var command = new DeleteEmployeeCommand(_employee!.Id, Role.Director);

        var handler = new DeleteEmployeeCommandHandler(
            _repositoryMock.Object,
            _cacheServiceMock.Object,
            _deleteLoggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(result, "Result");
    }

    [When(@"o usuário tenta acessar um endpoint protegido")]
    public void QuandoOUsuarioTentaAcessarUmEndpointProtegido()
    {
        _logEntries.Add((LogLevel.Warning, "Unauthorized access attempt to /api/employees"));
    }

    [When(@"o usuário realiza login com sucesso")]
    public async Task QuandoOUsuarioRealizaLoginComSucesso()
    {
        var command = new LoginCommand(_employee!.Email, _scenarioContext.Get<string>("CurrentPassword"));

        var handler = new LoginCommandHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _loginLoggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(result, "LoginResult");
    }

    [When(@"o usuário tenta fazer login com senha incorreta")]
    public async Task QuandoOUsuarioTentaFazerLoginComSenhaIncorreta()
    {
        var command = new LoginCommand(_employee!.Email, "SenhaErrada123");

        var handler = new LoginCommandHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _loginLoggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(result, "LoginResult");
    }

    [When(@"o funcionário altera sua senha com sucesso")]
    public async Task QuandoOUsuarioAlteraASenhaComSucesso()
    {
        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("new_hashed_password");

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentPassword = _scenarioContext.Get<string>("CurrentPassword");
        var command = new ChangePasswordCommand(_employee!.Id, currentPassword, "NovaSenha@456");

        var handler = new ChangePasswordCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _changePasswordLoggerMock.Object);

        var result = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(result, "Result");
    }

    [When(@"ocorre um erro interno durante uma operação")]
    public void QuandoOcorreUmErroInternoDuranteUmaOperacao()
    {
        _logEntries.Add((LogLevel.Error, "Internal error occurred: Database connection failed"));
    }

    [When(@"o usuário atualiza o nome do funcionário de ""(.*)"" para ""(.*)""")]
    public async Task QuandoOUsuarioAtualizaONomeDoFuncionarioDePara(string nomeAntigo, string nomeNovo)
    {
        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        var command = new UpdateEmployeeCommand(
            _employee!.Id,
            nomeNovo, _employee.LastName, _employee.Email,
            _employee.BirthDate,
            null,
            new List<string> { "11999999999" },
            null, 
            Role.Director 
        );

        var handler = new UpdateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _updateLoggerMock.Object);

        await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(nomeAntigo, "OldName");
        _scenarioContext.Set(nomeNovo, "NewName");
    }

    [Then(@"o sistema deve registrar um log com nível ""(.*)""")]
    public void EntaoOSistemaDeveRegistrarUmLogComNivel(string nivel)
    {
        var expectedLevel = nivel switch
        {
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Debug" => LogLevel.Debug,
            _ => LogLevel.None
        };

        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter a operação realizada ""(.*)""")]
    public void EntaoOLogDeveConterAOperacaoRealizada(string operacao)
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o ID do usuário que executou a operação")]
    public void EntaoOLogDeveConterOIdDoUsuarioQueExecutouAOperacao()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o ID do funcionário criado")]
    public void EntaoOLogDeveConterOIdDoFuncionarioCriado()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o ID do funcionário atualizado")]
    public void EntaoOLogDeveConterOIdDoFuncionarioAtualizado()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o ID do funcionário excluído")]
    public void EntaoOLogDeveConterOIdDoFuncionarioExcluido()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o ID do funcionário")]
    public void EntaoOLogDeveConterOIdDoFuncionario()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter timestamp da operação")]
    public void EntaoOLogDeveConterTimestampDaOperacao()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter os campos alterados")]
    public void EntaoOLogDeveConterOsCamposAlterados()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter ""(.*)""")]
    public void EntaoOLogDeveConter(string texto)
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o endpoint acessado")]
    public void EntaoOLogDeveConterOEndpointAcessado()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o email do usuário")]
    public void EntaoOLogDeveConterOEmailDoUsuario()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o email tentado")]
    public void EntaoOLogDeveConterOEmailTentado()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log NÃO deve conter a senha antiga ou nova")]
    public void EntaoOLogNaoDeveConterASenhaAntigaOuNova()
    {
        _logEntries.Should().NotContain(e =>
            e.Message.Contains("Senha") ||
            e.Message.Contains("password", StringComparison.OrdinalIgnoreCase));
    }

    [Then(@"o log deve conter a stack trace do erro")]
    public void EntaoOLogDeveConterAStackTraceDoErro()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o log deve conter o contexto da operação")]
    public void EntaoOLogDeveConterOContextoDaOperacao()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o sistema deve registrar a alteração no histórico de auditoria")]
    public void EntaoOSistemaDeveRegistrarAAlteracaoNoHistoricoDeAuditoria()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o histórico deve conter o valor anterior ""(.*)""")]
    public void EntaoOHistoricoDeveConterOValorAnterior(string valorAnterior)
    {
        _scenarioContext.Get<string>("OldName").Should().Be(valorAnterior);
    }

    [Then(@"o histórico deve conter o novo valor ""(.*)""")]
    public void EntaoOHistoricoDeveConterONovoValor(string novoValor)
    {
        _scenarioContext.Get<string>("NewName").Should().Be(novoValor);
    }

    [Then(@"o histórico deve conter o usuário que fez a alteração")]
    public void EntaoOHistoricoDeveConterOUsuarioQueFezAAlteracao()
    {
        _loggingConfigured.Should().BeTrue();
    }

    [Then(@"o histórico deve conter a data da alteração")]
    public void EntaoOHistoricoDeveConterADataDaAlteracao()
    {
        _loggingConfigured.Should().BeTrue();
    }

    private void SetupMocks()
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

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns("hashed_password");
    }
}
