# Boas Práticas e Padrões

## Princípios SOLID

### Single Responsibility Principle (SRP)

**Cada classe deve ter apenas uma responsabilidade.**

✅ **Bom**:
```csharp
// Uma responsabilidade: gerenciar repositório de Employee
public class EmployeeRepository : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id) { }
    public async Task AddAsync(Employee employee) { }
}

// Outra responsabilidade: validar Employee
public class EmployeeValidator : AbstractValidator<Employee>
{
    public EmployeeValidator() { }
}
```

❌ **Ruim**:
```csharp
public class EmployeeService
{
    public async Task<Employee> GetEmployee(Guid id) { }
    public async Task SaveEmployee(Employee employee) { }
    public bool ValidateEmployee(Employee employee) { }
    public void SendEmail(Employee employee) { }
    public void GenerateReport(Employee employee) { }
}
```

### Open/Closed Principle (OCP)

**Aberto para extensão, fechado para modificação.**

✅ **Bom**:
```csharp
public interface INotificationService
{
    Task SendAsync(string message);
}

public class EmailNotificationService : INotificationService { }
public class SmsNotificationService : INotificationService { }
public class PushNotificationService : INotificationService { }
```

### Liskov Substitution Principle (LSP)

**Subtipos devem ser substituíveis por seus tipos base.**

✅ **Bom**:
```csharp
public abstract class Entity
{
    public virtual void Delete(Guid? deletedBy) { }
}

public class Employee : Entity
{
    public override void Delete(Guid? deletedBy)
    {
        base.Delete(deletedBy);
        RaiseDomainEvent(new EmployeeDeletedEvent(Id));
    }
}
```

### Interface Segregation Principle (ISP)

**Clientes não devem depender de interfaces que não usam.**

✅ **Bom**:
```csharp
public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
}

public interface IWriteRepository<T>
{
    Task AddAsync(T entity);
    void Update(T entity);
}
```

❌ **Ruim**:
```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(Guid id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> CountAsync();
    // ... muitos métodos que nem todos os clientes precisam
}
```

### Dependency Inversion Principle (DIP)

**Dependa de abstrações, não de implementações.**

✅ **Bom**:
```csharp
public class CreateEmployeeCommandHandler
{
    private readonly IEmployeeRepository _repository; // Abstração
    private readonly IUnitOfWork _unitOfWork; // Abstração
    
    public CreateEmployeeCommandHandler(
        IEmployeeRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
}
```

## Domain-Driven Design (DDD)

### Agregados

**Employee é um agregado raiz que gerencia PhoneNumbers.**

```csharp
public class Employee : Entity // Agregado Raiz
{
    private readonly List<PhoneNumber> _phoneNumbers = [];
    public IReadOnlyCollection<PhoneNumber> PhoneNumbers => _phoneNumbers.AsReadOnly();
    
    public void AddPhone(PhoneNumber phone) => _phoneNumbers.Add(phone);
    public void ClearPhones() => _phoneNumbers.Clear();
}

public class PhoneNumber : Entity // Entidade dependente
{
    public Guid EmployeeId { get; private set; }
    // Não pode existir sem Employee
}
```

### Value Objects

**Objetos sem identidade, definidos por seus valores.**

```csharp
public record Address(
    string Street,
    string City,
    string State,
    string ZipCode);

// Imutável, comparação por valor
var address1 = new Address("Rua A", "SP", "SP", "01000-000");
var address2 = new Address("Rua A", "SP", "SP", "01000-000");
address1 == address2; // true
```

### Domain Events

**Comunicação entre agregados.**

```csharp
// Levantar evento
employee.RaiseDomainEvent(new EmployeeCreatedEvent(employee.Id));

// Handler reage
public class EmployeeCreatedEventHandler : INotificationHandler<EmployeeCreatedEvent>
{
    public async Task Handle(EmployeeCreatedEvent notification, CancellationToken ct)
    {
        // Enviar email, criar auditoria, etc.
    }
}
```

### Linguagem Ubíqua

**Código reflete a linguagem do negócio.**

✅ **Bom**:
```csharp
public class Employee
{
    public Guid? ManagerId { get; private set; }
    public Employee? Manager { get; private set; }
    public IReadOnlyCollection<Employee> Subordinates { get; }
    
    public bool CanCreateEmployeeWithRole(Role targetRole) { }
}
```

❌ **Ruim**:
```csharp
public class User
{
    public Guid? ParentId { get; set; }
    public User? Parent { get; set; }
    public List<User> Children { get; set; }
    
    public bool CheckPermission(int level) { }
}
```

## Clean Architecture

### Regra de Dependência

**Dependências apontam para dentro (Domain).**

```
API → Application → Domain
Infrastructure → Application → Domain
```

✅ **Domain nunca depende de outras camadas**

### Separação de Responsabilidades

- **Domain**: Regras de negócio
- **Application**: Casos de uso, orquestração
- **Infrastructure**: Detalhes técnicos
- **API**: Apresentação, HTTP

## CQRS

### Separar Commands e Queries

✅ **Bom**:
```csharp
// Command - Altera estado
public record CreateEmployeeCommand(...) : IRequest<Result<EmployeeResponse>>;

// Query - Apenas leitura
public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeResponse?>;
```

### Otimizar Separadamente

```csharp
// Query pode usar projeções diretas
public async Task<EmployeeResponse?> Handle(GetEmployeeByIdQuery request, ...)
{
    return await _context.Employees
        .Where(e => e.Id == request.Id)
        .Select(e => new EmployeeResponse(e.Id, e.FirstName, ...))
        .FirstOrDefaultAsync();
}
```

## Result Pattern

**Evitar exceções para fluxo de controle.**

✅ **Bom**:
```csharp
var result = Employee.Create(...);
if (result.IsFailure)
    return BadRequest(result.Error);

var employee = result.Value;
```

❌ **Ruim**:
```csharp
try
{
    var employee = new Employee(...);
    if (!employee.IsValid())
        throw new ValidationException("Invalid");
}
catch (ValidationException ex)
{
    return BadRequest(ex.Message);
}
```

## Validações

### Domain vs Application

**Domain**: Regras de negócio invariantes
```csharp
public static Result<Employee> Create(...)
{
    if (string.IsNullOrWhiteSpace(firstName))
        return Result<Employee>.Failure(Error.Validation(...));
}
```

**Application**: Validações de entrada
```csharp
public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.Email).EmailAddress();
    }
}
```

## Async/Await

### Sempre Async para I/O

✅ **Bom**:
```csharp
public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _context.Employees
        .FirstOrDefaultAsync(e => e.Id == id, ct);
}
```

❌ **Ruim**:
```csharp
public Employee? GetById(Guid id)
{
    return _context.Employees
        .FirstOrDefault(e => e.Id == id);
}
```

### CancellationToken

**Sempre passar CancellationToken.**

```csharp
public async Task<Result> Handle(
    CreateEmployeeCommand request,
    CancellationToken cancellationToken) // ✅
{
    await _repository.AddAsync(employee, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

## Imutabilidade

### Records para DTOs

```csharp
public record EmployeeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email);
```

### Private Setters

```csharp
public class Employee
{
    public string FirstName { get; private set; }
    public string Email { get; private set; }
    
    public Result Update(string firstName, string email)
    {
        FirstName = firstName;
        Email = email;
        return Result.Success();
    }
}
```

## Testes

### AAA Pattern

```csharp
[Fact]
public void Employee_Create_WithValidData_ShouldSucceed()
{
    // Arrange
    var firstName = "John";
    var lastName = "Doe";
    
    // Act
    var result = Employee.Create(firstName, lastName, ...);
    
    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.FirstName.Should().Be(firstName);
}
```

### Um Assert por Teste

✅ **Bom**:
```csharp
[Fact]
public void Employee_Create_ShouldSetFirstName()
{
    var result = Employee.Create("John", ...);
    result.Value.FirstName.Should().Be("John");
}

[Fact]
public void Employee_Create_ShouldSetEmail()
{
    var result = Employee.Create(..., email: "john@test.com");
    result.Value.Email.Should().Be("john@test.com");
}
```

## Logging

### Structured Logging

✅ **Bom**:
```csharp
_logger.LogInformation(
    "Employee created: {EmployeeId} - {Email}",
    employee.Id,
    employee.Email);
```

❌ **Ruim**:
```csharp
_logger.LogInformation(
    $"Employee created: {employee.Id} - {employee.Email}");
```

## Segurança

### Nunca Expor Senhas

```csharp
public class Employee
{
    public string PasswordHash { get; private set; } // ✅ Hash, não senha
    
    public Result UpdatePassword(string passwordHash) // ✅ Recebe hash
    {
        PasswordHash = passwordHash;
        return Result.Success();
    }
}
```

### Validar Permissões

```csharp
if (request.CurrentUserRole <= request.Role)
    return Result.Failure(Error.Forbidden("Sem permissão"));
```

## Performance

### Paginação

```csharp
var employees = await _context.Employees
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

### Eager Loading

```csharp
var employee = await _context.Employees
    .Include(e => e.PhoneNumbers)
    .Include(e => e.Manager)
    .FirstOrDefaultAsync(e => e.Id == id);
```

### Projeções

```csharp
var employees = await _context.Employees
    .Select(e => new EmployeeResponse(e.Id, e.FirstName, e.LastName))
    .ToListAsync();
```

## Próximos Passos

- [Arquitetura](02-ARQUITETURA.md)
- [Domínio](03-DOMINIO.md)
- [Aplicação](04-APLICACAO.md)

