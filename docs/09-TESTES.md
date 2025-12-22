# Testes

## Estratégia de Testes

O projeto implementa uma estratégia abrangente de testes usando **BDD (Behavior-Driven Development)** com SpecFlow e testes unitários com xUnit.

**Localização**: `tests/EmployeeManagement.Tests`

## Stack de Testes

- **xUnit** - Framework de testes unitários
- **SpecFlow** - BDD com Gherkin (Given-When-Then)
- **Moq** - Framework de mocking
- **FluentAssertions** - Assertions fluentes e legíveis
- **Bogus** - Geração de dados fake
- **EF Core InMemory** - Banco de dados em memória para testes

## Estrutura

```
tests/EmployeeManagement.Tests/
├── Features/                           # Arquivos .feature (Gherkin)
│   ├── Autenticacao.feature
│   ├── CriarFuncionario.feature
│   ├── AtualizarFuncionario.feature
│   ├── ExcluirFuncionario.feature
│   ├── ListarFuncionarios.feature
│   ├── AlterarSenha.feature
│   ├── HierarquiaPermissoes.feature
│   ├── Validacoes.feature
│   ├── LoggingAuditoria.feature
│   └── PerformanceEscalabilidade.feature
├── StepDefinitions/                    # Implementação dos steps
│   ├── AutenticacaoStepDefinitions.cs
│   ├── CriarFuncionarioStepDefinitions.cs
│   └── ...
├── UnitTests/                          # Testes unitários
│   ├── EmployeeTests.cs
│   ├── CreateEmployeeCommandHandlerTests.cs
│   └── ...
├── Fixtures/
│   └── MockFactory.cs
├── Helpers/
│   └── TestHelper.cs
└── Hooks/
    └── TestHooks.cs
```

## Testes BDD com SpecFlow

### Exemplo de Feature (Gherkin)

```gherkin
# language: pt-BR
Funcionalidade: Criação de Funcionário
  Como um usuário autorizado do sistema
  Eu quero cadastrar novos funcionários
  Para que eles possam acessar o sistema

  @funcionario @criar @sucesso
  Cenário: Criar funcionário com dados válidos
    Dado que o usuário está autenticado como "Director"
    E que não existe funcionário com documento "12345678900"
    E que existe um gestor cadastrado com ID válido
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 201
    E o sistema deve retornar os dados do funcionário criado
    E o funcionário deve ter um ID único gerado
    E a senha do funcionário deve estar hasheada no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário menor de idade
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome  | Sobrenome | Email            | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | Pedro | Costa     | pedro@supply.com | 98765432100 | 2010-06-15     | 11666666666 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "O funcionário deve ter pelo menos 18 anos"
    E o funcionário não deve ser criado no banco de dados
```

### Step Definitions

```csharp
[Binding]
public class CriarFuncionarioStepDefinitions
{
    private readonly ScenarioContext _scenarioContext;
    private HttpResponseMessage? _response;
    private EmployeeResponse? _createdEmployee;
    
    [Given(@"que o usuário está autenticado como ""(.*)""")]
    public void DadoQueOUsuarioEstaAutenticadoComo(string role)
    {
        var token = TestHelper.GenerateJwtToken(role);
        _scenarioContext["AuthToken"] = token;
    }
    
    [Given(@"que não existe funcionário com documento ""(.*)""")]
    public async Task DadoQueNaoExisteFuncionarioComDocumento(string documento)
    {
        var employee = await _repository.GetByDocumentNumberAsync(documento);
        employee.Should().BeNull();
    }
    
    [When(@"o usuário cria um novo funcionário com:")]
    public async Task QuandoOUsuarioCriaUmNovoFuncionarioCom(Table table)
    {
        var row = table.Rows[0];
        var request = new CreateEmployeeRequest
        {
            FirstName = row["Nome"],
            LastName = row["Sobrenome"],
            Email = row["Email"],
            DocumentNumber = row["Documento"],
            BirthDate = DateTime.Parse(row["DataNascimento"]),
            PhoneNumbers = row["Telefones"].Split(',').ToList(),
            Role = Enum.Parse<Role>(row["Permissao"]),
            Password = row["Senha"]
        };
        
        _response = await _httpClient.PostAsJsonAsync("/api/employees", request);
        _scenarioContext["Response"] = _response;
    }
    
    [Then(@"o sistema deve retornar status (.*)")]
    public void EntaoOSistemaDeveRetornarStatus(int statusCode)
    {
        _response.Should().NotBeNull();
        ((int)_response!.StatusCode).Should().Be(statusCode);
    }
    
    [Then(@"o funcionário deve ter um ID único gerado")]
    public void EntaoOFuncionarioDeveTerUmIDUnicoGerado()
    {
        _createdEmployee.Should().NotBeNull();
        _createdEmployee!.Id.Should().NotBeEmpty();
    }
}
```

## Testes Unitários

### Teste de Entidade (Domain)

```csharp
public class EmployeeTests
{
    [Fact]
    public void Employee_Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "john.doe@test.com";
        var document = "12345678900";
        var birthDate = DateTime.Now.AddYears(-25);
        var phones = new[] { "11999999999" };
        
        // Act
        var result = Employee.Create(
            firstName, lastName, email, document,
            birthDate, "hashedPassword", Role.Employee,
            null, phones);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be(firstName);
        result.Value.Email.Should().Be(email.ToLower());
        result.Value.PhoneNumbers.Should().HaveCount(1);
    }
    
    [Fact]
    public void Employee_Create_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        
        // Act
        var result = Employee.Create(
            "John", "Doe", invalidEmail, "12345678900",
            DateTime.Now.AddYears(-25), "hashedPassword",
            Role.Employee, null, new[] { "11999999999" });
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Validation");
    }
    
    [Theory]
    [InlineData(Role.Director, Role.Leader, true)]
    [InlineData(Role.Director, Role.Director, false)]
    [InlineData(Role.Leader, Role.Employee, true)]
    [InlineData(Role.Employee, Role.Leader, false)]
    public void Employee_CanCreateEmployeeWithRole_ShouldRespectHierarchy(
        Role currentRole, Role targetRole, bool expected)
    {
        // Arrange
        var employee = CreateValidEmployee(currentRole);
        
        // Act
        var canCreate = employee.CanCreateEmployeeWithRole(targetRole);
        
        // Assert
        canCreate.Should().Be(expected);
    }
}
```

### Teste de Handler (Application)

```csharp
public class CreateEmployeeCommandHandlerTests
{
    private readonly Mock<IEmployeeRepository> _mockRepository;
    private readonly Mock<IIdentityService> _mockIdentityService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly CreateEmployeeCommandHandler _handler;
    
    public CreateEmployeeCommandHandlerTests()
    {
        _mockRepository = new Mock<IEmployeeRepository>();
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        
        _handler = new CreateEmployeeCommandHandler(
            _mockRepository.Object,
            _mockIdentityService.Object,
            _mockUnitOfWork.Object);
    }
    
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateEmployee()
    {
        // Arrange
        var command = new CreateEmployeeCommand(
            "John", "Doe", "john@test.com", "12345678900",
            DateTime.Now.AddYears(-25), "Password@123",
            Role.Employee, null, new List<string> { "11999999999" },
            Role.Director);
        
        _mockRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
        
        _mockRepository
            .Setup(r => r.GetByDocumentNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Employee?)null);
        
        _mockIdentityService
            .Setup(s => s.HashPassword(It.IsAny<string>()))
            .Returns("hashedPassword");
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("john@test.com");
        
        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Once);
        
        _mockUnitOfWork.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Handle_DuplicateEmail_ShouldReturnConflict()
    {
        // Arrange
        var command = new CreateEmployeeCommand(...);
        
        var existingEmployee = CreateValidEmployee();
        _mockRepository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingEmployee);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Employee.Conflict");
        result.Error.Description.Should().Contain("Email já cadastrado");
        
        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<Employee>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
```

## Fixtures e Helpers

### MockFactory

```csharp
public static class MockFactory
{
    public static Employee CreateValidEmployee(
        Role role = Role.Employee,
        string? email = null)
    {
        var faker = new Faker("pt_BR");
        
        var result = Employee.Create(
            faker.Name.FirstName(),
            faker.Name.LastName(),
            email ?? faker.Internet.Email(),
            faker.Random.Replace("###########"),
            faker.Date.Past(30, DateTime.Now.AddYears(-18)),
            "hashedPassword",
            role,
            null,
            new[] { faker.Phone.PhoneNumber("11#########") });
        
        return result.Value;
    }
}
```

### TestHelper

```csharp
public static class TestHelper
{
    public static string GenerateJwtToken(string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role)
        };
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-secret-key"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

## Executar Testes

### Todos os Testes

```bash
cd tests/EmployeeManagement.Tests
dotnet test
```

### Com Cobertura

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Filtrar por Tag

```bash
dotnet test --filter "Category=funcionario"
dotnet test --filter "Category=criar&Category=sucesso"
```

### Apenas Testes Unitários

```bash
dotnet test --filter "FullyQualifiedName~UnitTests"
```

## Cobertura de Testes

O projeto visa manter cobertura mínima de:
- **Domain**: 90%+
- **Application**: 80%+
- **Infrastructure**: 70%+

## Boas Práticas

✅ Testes isolados e independentes  
✅ Usar mocks para dependências externas  
✅ Nomenclatura clara (Given-When-Then)  
✅ Um assert por teste (quando possível)  
✅ Testar casos de sucesso e falha  
✅ Usar FluentAssertions para legibilidade  
✅ Gerar dados com Bogus  
✅ Limpar estado entre testes  

## Próximos Passos

- [Guia de Desenvolvimento](13-GUIA-DESENVOLVIMENTO.md)
- [Boas Práticas](14-BOAS-PRATICAS.md)

