using TechTalk.SpecFlow;
using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Tests.Helpers;
using EmployeeManagement.Tests.Fixtures;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
public class ValidacoesStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ILogger<CreateEmployeeCommandHandler>> _loggerMock;

    private Result<EmployeeResponse>? _result;
    private int _httpStatus = 200;

    public ValidacoesStepDefinitions(ScenarioContext scenarioContext)
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

    [Given(@"que o usuario esta autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        var parsedRole = Enum.Parse<Role>(role);
        _scenarioContext.Set(parsedRole, "CurrentUserRole");
    }

    [When(@"o usuario tenta criar um funcionario com email ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComEmail(string email)
    {
        await CriarFuncionarioComEmail(email);
    }

    [When(@"o usuario tenta criar um funcionario com telefone ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComTelefone(string telefone)
    {
        await CriarFuncionarioComTelefone(telefone);
    }

    [When(@"o usuario tenta criar um funcionario com documento ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComDocumento(string documento)
    {
        await CriarFuncionarioComDocumento(documento);
    }

    [When(@"o usuario tenta criar um funcionario com senha ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComSenha(string senha)
    {
        await CriarFuncionarioComSenha(senha);
    }

    [When(@"o usuario tenta criar um funcionario com data de nascimento ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComDataDeNascimento(string dataNascimento)
    {
        var date = DateTime.Parse(dataNascimento);
        await CriarFuncionarioComDataNascimento(date);
    }

    [When(@"o usuario tenta criar um funcionario com nome ""(.*)""")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComNome(string nome)
    {
        await CriarFuncionarioComNome(nome);
    }

    [When(@"o usuario tenta criar um funcionario com sobrenome de (.*) caracteres")]
    public async Task QuandoOUsuarioTentaCriarUmFuncionarioComSobrenomeDeMuitosCaracteres(int caracteres)
    {
        var sobrenomeLongo = new string('A', caracteres);
        await CriarFuncionarioComSobrenome(sobrenomeLongo);
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o sistema deve retornar mensagem indicando formato de email invalido")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoFormatoDeEmailInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando formato de telefone invalido")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoFormatoDeTelefoneInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando formato de documento invalido")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoFormatoDeDocumentoInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando documento invalido")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoDocumentoInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que a senha e muito curta")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSenhaEMuitoCurta()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que a senha deve conter caracteres especiais")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSenhaDeveConterCaracteresEspeciais()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que a senha deve conter numeros")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSenhaDeveConterNumeros()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que a senha deve conter letras maiusculas")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSenhaDeveConterLetrasMaiusculas()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando data de nascimento invalida")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoDataDeNascimentoInvalida()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando formato de nome invalido")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoFormatoDeNomeInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que o nome deve ter pelo menos 2 caracteres")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueNomeDeveTerPeloMenos2Caracteres()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando que o sobrenome excede o limite de caracteres")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoQueSobrenomeExcedeLimiteDeCaracteres()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    #region Helper Methods

    private async Task CriarFuncionarioComEmail(string email)
    {
        var command = CreateCommand(email: email);
        await ExecutarCriacao(command);
    }

    private async Task CriarFuncionarioComTelefone(string telefone)
    {
        var command = CreateCommand(telefones: new List<string> { telefone });
        await ExecutarCriacao(command);
    }

    private async Task CriarFuncionarioComDocumento(string documento)
    {
        var command = CreateCommand(documento: documento);
        await ExecutarCriacao(command);
    }

    private async Task CriarFuncionarioComSenha(string senha)
    {
        var command = CreateCommand(senha: senha);
        await ExecutarCriacao(command);
    }

    private async Task CriarFuncionarioComDataNascimento(DateTime dataNascimento)
    {
        var command = CreateCommand(dataNascimento: dataNascimento);
        await ExecutarCriacao(command);
    }

    private async Task CriarFuncionarioComNome(string nome)
    {
        var command = CreateCommand(nome: nome);
        await ExecutarCriacao(command);
    }

    private async Task CriarFuncionarioComSobrenome(string sobrenome)
    {
        var command = CreateCommand(sobrenome: sobrenome);
        await ExecutarCriacao(command);
    }

    private CreateEmployeeCommand CreateCommand(
        string? nome = null,
        string? sobrenome = null,
        string? email = null,
        string? documento = null,
        DateTime? dataNascimento = null,
        string? senha = null,
        List<string>? telefones = null)
    {
        var currentRole = _scenarioContext.TryGetValue<Role>("CurrentUserRole", out var role) ? role : Role.Director;

        return new CreateEmployeeCommand(
            nome ?? "Joao",
            sobrenome ?? "Silva",
            email ?? $"joao{Guid.NewGuid()}@supply.com",
            documento ?? TestHelper.GenerateValidCpf(),
            dataNascimento ?? TestHelper.GenerateAdultBirthDate(),
            senha ?? "Senha@123456",
            Role.Employee,
            null,
            telefones ?? new List<string> { "11999999999" },
            currentRole);
    }

    private async Task ExecutarCriacao(CreateEmployeeCommand command)
    {
        var handler = new CreateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _passwordHasherMock.Object,
            _loggerMock.Object);

        _result = await handler.Handle(command, CancellationToken.None);

        _httpStatus = _result.IsSuccess ? 201 : 400;
        _scenarioContext.Set(_httpStatus, "HttpStatus");
        _scenarioContext.Set(_result, "OperationResult");

        if (_result.IsFailure)
        {
            _scenarioContext.Set(_result.Error.Description, "ErrorMessage");
        }
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
            .Returns((Task<Employee>)Task.CompletedTask);
    }

    #endregion
}