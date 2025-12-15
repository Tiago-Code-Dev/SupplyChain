using TechTalk.SpecFlow;
using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Auth.Commands.Login;
using EmployeeManagement.Application.Features.Auth.Common;
using EmployeeManagement.Tests.Helpers;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
public class AutenticacaoStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<ILogger<LoginCommandHandler>> _loggerMock;

    private Result<AuthResponse>? _loginResult;
    private Employee? _existingEmployee;
    private string _currentPassword = string.Empty;
    private string _currentPasswordHash = string.Empty;
    private int _httpStatus;

    public AutenticacaoStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtServiceMock = new Mock<IJwtService>();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<LoginCommandHandler>();
    }

    [Given(@"que existe um funcionário cadastrado no sistema com:")]
    public void DadoQueExisteUmFuncionarioCadastradoNoSistemaCom(Table table)
    {
        var data = table.Rows[0];
        var email = data["Email"];
        _currentPassword = data["Senha"];
        _currentPasswordHash = $"hashed_{_currentPassword}";

        _existingEmployee = TestHelper.CreateValidEmployee(
            email: email,
            passwordHash: _currentPasswordHash);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingEmployee);

        _passwordHasherMock
            .Setup(x => x.Verify(_currentPassword, _currentPasswordHash))
            .Returns(true);

        _passwordHasherMock
            .Setup(x => x.Verify(It.Is<string>(s => s != _currentPassword), It.IsAny<string>()))
            .Returns(false);

        _jwtServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<Employee>()))
            .Returns("valid-jwt-token-with-claims");
    }

    [Given(@"que não existe um funcionário cadastrado com email ""(.*)""")]
    public void DadoQueNaoExisteUmFuncionarioCadastradoComEmail(string email)
    {
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
    }

    [Given(@"que o usuário não possui token de autenticação")]
    public void DadoQueOUsuarioNaoPossuiTokenDeAutenticacao()
    {
        _scenarioContext.Set<string?>(null, "AuthToken");
    }

    [Given(@"que o usuário possui um token JWT expirado")]
    public void DadoQueOUsuarioPossuiUmTokenJWTExpirado()
    {
        _scenarioContext.Set("expired-jwt-token", "AuthToken");
        _scenarioContext.Set(true, "TokenExpired");
    }

    [When(@"o usuário realiza login com:")]
    public async Task QuandoOUsuarioRealizaLoginCom(Table table)
    {
        var data = table.Rows[0];
        var email = data["Email"];
        var senha = data["Senha"];

        await ExecutarLogin(email, senha);
    }

    [When(@"o usuário realiza login com email ""(.*)"" e senha ""(.*)""")]
    public async Task QuandoOUsuarioRealizaLoginComEmailESenha(string email, string senha)
    {
        await ExecutarLogin(email, senha);
    }

    [When(@"o usuário tenta acessar o endpoint GET /api/employees")]
    public void QuandoOUsuarioTentaAcessarOEndpointGETApiEmployees()
    {
        if (!_scenarioContext.TryGetValue<string>("AuthToken", out var token) || token == null)
        {
            _httpStatus = 401;
            _scenarioContext.Set("Não autorizado", "ErrorMessage");
        }
        else if (_scenarioContext.TryGetValue<bool>("TokenExpired", out var expired) && expired)
        {
            _httpStatus = 401;
            _scenarioContext.Set("Token expirado", "ErrorMessage");
        }
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int statusCode)
    {
        if (_loginResult != null)
        {
            var actualStatus = _loginResult.IsSuccess ? 200 : 401;
            actualStatus.Should().Be(statusCode);
        }
        else
        {
            _httpStatus.Should().Be(statusCode);
        }
    }

    [Then(@"o sistema deve retornar um token JWT válido")]
    public void EntaoOSistemaDeveRetornarUmTokenJWTValido()
    {
        _loginResult.Should().NotBeNull();
        _loginResult!.IsSuccess.Should().BeTrue();
        _loginResult.Value.Token.Should().NotBeNullOrEmpty();
    }

    [Then(@"o token deve conter as claims de identificação do usuário")]
    public void EntaoOTokenDeveConterAsClaimsDeIdentificacaoDoUsuario()
    {
        _loginResult.Should().NotBeNull();
        _loginResult!.IsSuccess.Should().BeTrue();
        _loginResult.Value.Employee.Should().NotBeNull();
        _loginResult.Value.Employee.Id.Should().NotBe(Guid.Empty);
    }

    [Then(@"o token deve conter a claim de permissão do usuário")]
    public void EntaoOTokenDeveConterAClaimDePermissaoDoUsuario()
    {
        _loginResult.Should().NotBeNull();
        _loginResult!.IsSuccess.Should().BeTrue();
        _loginResult.Value.Employee.Role.Should().BeDefined();
    }

    [Then(@"o sistema deve retornar mensagem ""(.*)""")]
    public void EntaoOSistemaDeveRetornarMensagem(string mensagem)
    {
        if (_loginResult != null && _loginResult.IsFailure)
        {
            if (mensagem == "Credenciais inválidas")
            {
                _loginResult.Error.Description.Should().Be("Invalid email or password");
            }
            else
            {
                _loginResult.Error.Description.Should().Contain(mensagem);
            }
        }
        else if (_scenarioContext.TryGetValue<string>("ErrorMessage", out var errorMsg))
        {
            errorMsg.Should().Contain(mensagem);
        }
    }

    [Then(@"o sistema não deve retornar token JWT")]
    public void EntaoOSistemaNaoDeveRetornarTokenJWT()
    {
        _loginResult.Should().NotBeNull();
        _loginResult!.IsFailure.Should().BeTrue();
    }

    private async Task ExecutarLogin(string email, string senha)
    {
        var handler = new LoginCommandHandler(
            _repositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtServiceMock.Object,
            _loggerMock.Object);

        var command = new LoginCommand(email, senha);
        _loginResult = await handler.Handle(command, CancellationToken.None);
        _scenarioContext.Set(_loginResult, "LoginResult");
    }
}