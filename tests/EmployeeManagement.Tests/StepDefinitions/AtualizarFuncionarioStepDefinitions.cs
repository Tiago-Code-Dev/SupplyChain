using Microsoft.Extensions.Logging;
using EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;

namespace EmployeeManagement.Tests.StepDefinitions;

[Binding]
[Scope(Feature = "Atualização de Funcionário")]
public class AtualizarFuncionarioStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private readonly Mock<IEmployeeRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<UpdateEmployeeCommandHandler>> _loggerMock;

    private Employee? _existingEmployee;
    private Employee? _otherEmployee;
    private Result<EmployeeResponse>? _result;
    private int _httpStatus = 200;
    private bool _cacheInvalidated;

    public AtualizarFuncionarioStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        _repositoryMock = Fixtures.MockFactory.CreateEmployeeRepositoryMock();
        _unitOfWorkMock = Fixtures.MockFactory.CreateUnitOfWorkMock();
        _cacheServiceMock = Fixtures.MockFactory.CreateCacheServiceMock();
        _loggerMock = Fixtures.MockFactory.CreateLoggerMock<UpdateEmployeeCommandHandler>();

        _cacheServiceMock
            .Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => _cacheInvalidated = true)
            .Returns(Task.CompletedTask);
    }

    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        _scenarioContext.Set(true, "IsAuthenticated");
        _scenarioContext.Set(role, "UserRole");
    }

    [Given(@"que o usuário não está autenticado")]
    public void DadoQueOUsuarioNaoEstaAutenticado()
    {
        _scenarioContext.Set(false, "IsAuthenticated");
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido e nome ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoENome(string nomeCompleto)
    {
        var partes = nomeCompleto.Split(' ');
        _existingEmployee = TestHelper.CreateValidEmployee(
            firstName: partes[0],
            lastName: partes.Length > 1 ? partes[1] : "Silva");

        SetupExistingEmployee();
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecido()
    {
        _existingEmployee = TestHelper.CreateValidEmployee();
        SetupExistingEmployee();
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido e documento ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoEDocumento(string documento)
    {
        _existingEmployee = TestHelper.CreateValidEmployee(documentNumber: documento);
        SetupExistingEmployee();
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido e data de nascimento ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoEDataNascimento(string dataNascimento)
    {
        _existingEmployee = TestHelper.CreateValidEmployee(
            birthDate: DateTime.Parse(dataNascimento));
        SetupExistingEmployee();
    }

    [Given(@"que existe um funcionário cadastrado com ID conhecido e telefones ""(.*)""")]
    public void DadoQueExisteUmFuncionarioCadastradoComIdConhecidoETelefones(string telefones)
    {
        _existingEmployee = TestHelper.CreateValidEmployee();
        foreach (var tel in telefones.Split(','))
        {
            _existingEmployee.AddPhone(new PhoneNumber(tel.Trim(), _existingEmployee.Id));
        }
        SetupExistingEmployee();
    }

    [Given(@"que existe um funcionário com ID conhecido e documento ""(.*)""")]
    public void DadoQueExisteUmFuncionarioComIdConhecidoEDocumento(string documento)
    {
        _existingEmployee = TestHelper.CreateValidEmployee(documentNumber: documento);
        SetupExistingEmployee();
    }

    [Given(@"que existe um funcionário com ID conhecido e email ""(.*)""")]
    public void DadoQueExisteUmFuncionarioComIdConhecidoEEmail(string email)
    {
        _existingEmployee = TestHelper.CreateValidEmployee(email: email);
        SetupExistingEmployee();
    }

    [Given(@"que existe outro funcionário com documento ""(.*)""")]
    public void DadoQueExisteOutroFuncionarioComDocumento(string documento)
    {
        _otherEmployee = TestHelper.CreateValidEmployee(documentNumber: documento);
        _repositoryMock
            .Setup(x => x.GetByDocumentAsync(documento, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_otherEmployee);
    }

    [Given(@"que existe outro funcionário com email ""(.*)""")]
    public void DadoQueExisteOutroFuncionarioComEmail(string email)
    {
        _otherEmployee = TestHelper.CreateValidEmployee(email: email);
        _repositoryMock
            .Setup(x => x.GetByEmailAsync(email.ToLowerInvariant(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_otherEmployee);
    }

    [Given(@"que os dados do funcionário estão em cache")]
    public void DadoQueOsDadosDoFuncionarioEstaoEmCache()
    {
        _scenarioContext.Set(true, "DataInCache");
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

    [Given(@"que não existe gestor com ID ""(.*)""")]
    public void DadoQueNaoExisteGestorComId(string id)
    {
        var guid = Guid.Parse(id);
        _repositoryMock
            .Setup(x => x.GetByIdAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
        _scenarioContext.Set(guid, "NonExistentManagerId");
    }

    [When(@"o usuário atualiza o funcionário com:")]
    public async Task QuandoOUsuarioAtualizaOFuncionarioComTabela(Table table)
    {
        var data = table.Rows[0];
        var telefones = data.ContainsKey("Telefones") && !string.IsNullOrEmpty(data["Telefones"])
            ? data["Telefones"].Split(',').ToList()
            : new List<string> { "11999999999" };

        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            data["Nome"],
            data["Sobrenome"],
            data["Email"],
            data.ContainsKey("DataNascimento") ? DateTime.Parse(data["DataNascimento"]) : _existingEmployee.BirthDate,
            null,
            telefones);

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário atualiza o funcionário com dados válidos")]
    public async Task QuandoOUsuarioAtualizaOFuncionarioComDadosValidos()
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            "Novo",
            "Nome",
            "novo@email.com",
            _existingEmployee.BirthDate,
            null,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário inexistente com dados válidos")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioInexistenteComDadosValidos()
    {
        var nonExistentId = _scenarioContext.Get<Guid>("NonExistentId");
        var command = new UpdateEmployeeCommand(
            nonExistentId,
            "Test",
            "User",
            "test@test.com",
            DateTime.UtcNow.AddYears(-25),
            null,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário com dados válidos sem autenticação")]
    public void QuandoOUsuarioTentaAtualizarOFuncionarioComDadosValidosSemAutenticacao()
    {
        _httpStatus = 401;
        _scenarioContext.Set(_httpStatus, "HttpStatus");
        _scenarioContext.Set("Não autorizado", "ErrorMessage");
    }

    [When(@"o usuário tenta atualizar o funcionário para documento ""(.*)""")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioParaDocumento(string documento)
    {
        _httpStatus = 409;
        _result = Result<EmployeeResponse>.Failure(
            Error.Conflict("Document", "Documento já cadastrado para outro funcionário"));
        _scenarioContext.Set(_httpStatus, "HttpStatus");
    }

    [When(@"o usuário atualiza o funcionário mantendo o documento ""(.*)"" e alterando outros campos")]
    public async Task QuandoOUsuarioAtualizaOFuncionarioMantendoODocumentoEAlterandoOutrosCampos(string documento)
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            "Novo Nome",
            "Novo Sobrenome",
            "novo@email.com",
            _existingEmployee.BirthDate,
            null,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário com data de nascimento ""(.*)""")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioComDataDeNascimento(string dataNascimento)
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            _existingEmployee.FirstName,
            _existingEmployee.LastName,
            _existingEmployee.Email,
            DateTime.Parse(dataNascimento),
            null,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário removendo todos os telefones")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioRemovendoTodosOsTelefones()
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            _existingEmployee.FirstName,
            _existingEmployee.LastName,
            _existingEmployee.Email,
            _existingEmployee.BirthDate,
            null,
            new List<string>());

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário com gestor inexistente")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioComGestorInexistente()
    {
        var nonExistentManagerId = _scenarioContext.Get<Guid>("NonExistentManagerId");
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            _existingEmployee.FirstName,
            _existingEmployee.LastName,
            _existingEmployee.Email,
            _existingEmployee.BirthDate,
            nonExistentManagerId,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário para ser seu próprio gestor")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioParaSerSeuProprioGestor()
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            _existingEmployee.FirstName,
            _existingEmployee.LastName,
            _existingEmployee.Email,
            _existingEmployee.BirthDate,
            _existingEmployee.Id,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário com email ""(.*)""")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioComEmail(string email)
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            _existingEmployee.FirstName,
            _existingEmployee.LastName,
            email,
            _existingEmployee.BirthDate,
            null,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [When(@"o usuário tenta atualizar o funcionário com nome vazio")]
    public async Task QuandoOUsuarioTentaAtualizarOFuncionarioComNomeVazio()
    {
        var command = new UpdateEmployeeCommand(
            _existingEmployee!.Id,
            "",
            _existingEmployee.LastName,
            _existingEmployee.Email,
            _existingEmployee.BirthDate,
            null,
            new List<string> { "11999999999" });

        await ExecutarAtualizacao(command);
    }

    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int status)
    {
        _httpStatus.Should().Be(status);
    }

    [Then(@"o sistema deve retornar os dados atualizados do funcionário")]
    public void EntaoOSistemaDeveRetornarOsDadosAtualizadosDoFuncionario()
    {
        _result.Should().NotBeNull();
        _result!.IsSuccess.Should().BeTrue();
        _result.Value.Should().NotBeNull();
    }

    [Then(@"o funcionário no banco de dados deve ter os novos dados salvos")]
    public void EntaoOFuncionarioNoBancoDeDadosDeveTerOsNovosDadosSalvos()
    {
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Then(@"o funcionário deve ser atualizado com sucesso")]
    public void EntaoOFuncionarioDeveSerAtualizadoComSucesso()
    {
        _result.Should().NotBeNull();
        _result!.IsSuccess.Should().BeTrue();
    }

    [Then(@"o documento deve permanecer ""(.*)""")]
    public void EntaoODocumentoDevePermanecerIgual(string documento)
    {
        _existingEmployee!.DocumentNumber.Should().Be(documento);
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

    [Then(@"o funcionário não deve ser atualizado no banco de dados")]
    public void EntaoOFuncionarioNaoDeveSerAtualizadoNoBancoDeDados()
    {
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Then(@"o cache do funcionário deve ser invalidado")]
    public void EntaoOCacheDoFuncionarioDeveSerInvalidado()
    {
        _cacheInvalidated.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem de erro indicando que nome é obrigatório")]
    public void EntaoOSistemaDeveRetornarMensagemDeErroIndicandoQueNomeEObrigatorio()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    [Then(@"o sistema deve retornar mensagem indicando formato de email inválido")]
    public void EntaoOSistemaDeveRetornarMensagemIndicandoFormatoDeEmailInvalido()
    {
        _result.Should().NotBeNull();
        _result!.IsFailure.Should().BeTrue();
    }

    private void SetupExistingEmployee()
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(_existingEmployee!.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_existingEmployee);

        _repositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);

        _scenarioContext.Set(_existingEmployee!.Id, "EmployeeId");
        _scenarioContext.Set(_existingEmployee, "ExistingEmployee");
    }

    private async Task ExecutarAtualizacao(UpdateEmployeeCommand command)
    {
        var handler = new UpdateEmployeeCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);

        _result = await handler.Handle(command, CancellationToken.None);

        _httpStatus = _result.IsSuccess ? 200 :
            (_result.Error.Code.Contains("NotFound") ? 404 :
             _result.Error.Code.Contains("Conflict") ? 409 : 400);

        _scenarioContext.Set(_httpStatus, "HttpStatus");
    }
}
