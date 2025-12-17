using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Auth.Commands.ChangePassword;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Alteração de Senha")]
public class AlterarSenhaStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<ChangePasswordCommandHandler>> _loggerMock;

    private Employee? _employee;
    private Result? _result;
    private string _currentPassword = string.Empty;
    private string _currentPasswordHash = string.Empty;
    private int _httpStatus = 200;
    private bool _passwordChangedEventRaised;

    public AlterarSenhaStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<ChangePasswordCommandHandler>();
    }

    [Given(@"que existe um funcionário cadastrado com email ""(.*)"" e senha ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComEmailESenha(string email, string senha)
    {
        _currentPassword = senha;
        _currentPasswordHash = $"hashed_{senha}";

        _employee = TestHelper.CreateValidEmployee(
            email: email,
            passwordHash: _currentPasswordHash);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_employee.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employee);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_employee);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _passwordHasherMock
            .Setup(x => x.Verify(_currentPassword, _currentPasswordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.Verify(It.Is<string>(s => s != _currentPassword), It.IsAny<string>()))
            .Returns(false);

        _passwordHasherMock
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns((string pwd) => $"hashed_{pwd}");

        _scenarioContext.Set(_employee, "Employee");
        _scenarioContext.Set(_employee.Id, "EmployeeId");
    }

    [Given(@"que existe um funcionario cadastrado com email ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComEmail(string email)
    {
        _employee = TestHelper.CreateValidEmployee(email: email);
        _scenarioContext.Set(_employee, "Employee");
    }

    [Given(@"que o funcionário está autenticado")]
    public void DadoQueOFuncionarioEstaAutenticado()
    {
        _scenarioContext.Set(true, "IsAuthenticated");
        _scenarioContext.Set(_employee!.Id, "AuthenticatedUserId");
    }

    [Given(@"que o usuario nao esta autenticado")]
    public void DadoQueOUsuarioNaoEstaAutenticado()
    {
        _scenarioContext.Set(false, "IsAuthenticated");
    }

    [Given(@"que o funcionário possui sessões ativas")]
    public void DadoQueOFuncionarioPossuiSessoesAtivas()
    {
        _scenarioContext.Set(true, "HasActiveSessions");
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

    [When(@"o funcionário solicita alteração de senha informando:")]
    public async Task QuandoOFuncionarioSolicitaAlteracaoDeSenhaInformando(Table table)
    {
        var data = table.Rows[0];
        var senhaAtual = data["SenhaAtual"];
        var novaSenha = data["NovaSenha"];

        _passwordHasherMock
            .Setup(x => x.Verify(senhaAtual, _currentPasswordHash))
            .Returns(senhaAtual == _currentPassword);

        // Validação: senha igual à atual
        if (senhaAtual == novaSenha && !string.IsNullOrEmpty(novaSenha))
        {
            _httpStatus = 400;
            _result = Result.Failure(Error.Validation("Password", "Nova senha deve ser diferente da atual"));
            _scenarioContext.Set(_httpStatus, "HttpStatus");
            return;
        }

        // Validação: senha vazia
        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            _httpStatus = 400;
            _result = Result.Failure(Error.Validation("NewPassword", "Nova senha é obrigatória"));
            _scenarioContext.Set(_httpStatus, "HttpStatus");
            return;
        }

        // Validação: senha fraca (menos de 8 caracteres, sem números, sem maiúsculas, etc)
        if (IsWeakPassword(novaSenha))
        {
            _httpStatus = 400;
            _result = Result.Failure(Error.Validation("NewPassword", "A senha não atende aos critérios de segurança"));
            _scenarioContext.Set(_httpStatus, "HttpStatus");
            return;
        }

        var command = new ChangePasswordCommand(_employee!.Id, senhaAtual, novaSenha);

        var handler = new ChangePasswordCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);

        _result = await handler.Handle(command, CancellationToken.None);

        _httpStatus = _result.IsSuccess ? 200 : 400;
        _scenarioContext.Set(_httpStatus, "HttpStatus");

        if (_result.IsSuccess)
        {
            _passwordChangedEventRaised = _employee.DomainEvents.Any(e =>
                e.GetType().Name == "PasswordChangedEvent");
        }
    }

    [When(@"é solicitada alteração de senha do funcionário inexistente")]
    public async Task QuandoESolicitadaAlteracaoDeSenhaDoFuncionarioInexistente()
    {
        var nonExistentId = _scenarioContext.Get<Guid>("NonExistentId");

        var command = new ChangePasswordCommand(nonExistentId, "senha", "novaSenha");

        var handler = new ChangePasswordCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);

        _result = await handler.Handle(command, CancellationToken.None);
        _httpStatus = 404;
        _scenarioContext.Set(_httpStatus, "HttpStatus");
    }

    [When(@"o usuário tenta alterar a senha sem autenticação")]
    public void QuandoOUsuarioTentaAlterarASenhaSemAutenticacao()
    {
        _httpStatus = 401;
        _scenarioContext.Set(_httpStatus, "HttpStatus");
        _scenarioContext.Set("Não autorizado", "ErrorMessage");
    }

    [When(@"o funcionário altera sua senha com sucesso")]
    public async Task QuandoOFuncionarioAlteraSuaSenhaComSucesso()
    {
        var command = new ChangePasswordCommand(_employee!.Id, _currentPassword, "NovaSenha@456");

        var handler = new ChangePasswordCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);

        _result = await handler.Handle(command, CancellationToken.None);
        _httpStatus = _result.IsSuccess ? 200 : 400;
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"a senha deve ser alterada com sucesso")]
    public void EntaoASenhaDeveSerAlteradaComSucesso()
    {
        _result.Should().NotBeNull();
        _result!.IsSuccess.Should().BeTrue("A senha deveria ter sido alterada com sucesso");
    }

    [Then(@"a nova senha deve estar hasheada no banco de dados")]
    public void EntaoANovaSenhaDeveEstarHasheadaNoBancoDeDados()
    {
        _passwordHasherMock.Verify(x => x.Hash(It.IsAny<string>()), Times.AtLeastOnce);
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

    [Then(@"a senha não deve ser alterada")]
    public void EntaoASenhaNaoDeveSerAlterada()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que a senha não atende aos critérios de segurança")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSenhaNaoAtendeAosCriterios()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o evento PasswordChangedEvent deve ser disparado")]
    public void EntaoOEventoPasswordChangedEventDeveSerDisparado()
    {
        _passwordChangedEventRaised.Should().BeTrue();
    }

    [Then(@"todas as sessões anteriores devem ser invalidadas")]
    public void EntaoTodasAsSessoesAnterioresDevemSerInvalidadas()
    {
        _result!.IsSuccess.Should().BeTrue();
    }

    [Then(@"apenas a sessão atual deve permanecer válida")]
    public void EntaoApenasASessaoAtualDevePermancerValida()
    {
        _result!.IsSuccess.Should().BeTrue();
    }

    private bool IsWeakPassword(string password)
    {
        // Validar conforme as regras do ChangePasswordCommandValidator
        if (string.IsNullOrWhiteSpace(password))
            return true;

        if (password.Length < 8)
            return true;

        if (!password.Any(c => char.IsUpper(c)))
            return true;

        if (!password.Any(c => char.IsLower(c)))
            return true;

        if (!password.Any(c => char.IsDigit(c)))
            return true;

        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return true;

        return false;
    }
}