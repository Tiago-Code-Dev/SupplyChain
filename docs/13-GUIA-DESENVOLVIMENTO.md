# Guia de Desenvolvimento

## Pré-requisitos

### Obrigatórios

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)

### Recomendados

- **Visual Studio 2022** (17.8+) ou **VS Code** com extensão C#
- **SQL Server Management Studio** (SSMS) ou **Azure Data Studio**
- **Postman** ou **Insomnia** para testes de API

## Setup Inicial

### 1. Clonar Repositório

```bash
git clone https://github.com/seu-usuario/SupplyChain.git
cd SupplyChain
```

### 2. Restaurar Dependências

```bash
dotnet restore
```

### 3. Verificar Build

```bash
dotnet build
```

## Executando o Projeto

### Opção 1: Docker (Recomendado)

#### Com HTTPS

1. Gerar certificado:
```bash
# Windows
.\scripts\generate-dev-cert.ps1

# Linux/Mac
chmod +x ./scripts/generate-dev-cert.sh
./scripts/generate-dev-cert.sh
```

2. Iniciar containers:
```bash
docker-compose up --build
```

3. Acessar:
- API: http://localhost:5000 ou https://localhost:5001
- Swagger: http://localhost:5000/swagger

#### Apenas HTTP (Desenvolvimento Simplificado)

```bash
docker-compose -f docker-compose.yml -f docker-compose.http-only.yml up --build
```

### Opção 2: Localmente (sem Docker)

#### 1. Iniciar SQL Server

**Docker**:
```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=SqlServer@123" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

**Ou usar SQL Server local instalado**

#### 2. Configurar Connection String

Editar `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EmployeeManagementDb;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True"
  }
}
```

#### 3. Executar Migrations

```bash
cd src/EmployeeManagement/EmployeeManagement.Api
dotnet ef database update --project ../EmployeeManagement.Infrastructure
```

#### 4. Executar API

```bash
dotnet run
```

Acessar: https://localhost:5051/swagger

## Estrutura do Projeto

```
SupplyChain-main/
├── src/
│   ├── EmployeeManagement/
│   │   ├── EmployeeManagement.Api/          # 🌐 Camada de Apresentação
│   │   ├── EmployeeManagement.Application/  # 💼 Casos de Uso
│   │   ├── EmployeeManagement.Domain/       # 🎯 Regras de Negócio
│   │   └── EmployeeManagement.Infrastructure/ # 🔧 Infraestrutura
│   └── Shared/
│       ├── Shared.Contracts/
│       └── Shared.CrossCutting/
├── tests/
│   └── EmployeeManagement.Tests/            # ✅ Testes
├── docs/                                     # 📚 Documentação
├── scripts/                                  # 🛠️ Scripts utilitários
└── docker-compose.yml                        # 🐳 Docker
```

## Convenções de Código

### Nomenclatura

- **Classes**: PascalCase (`EmployeeService`)
- **Métodos**: PascalCase (`GetEmployeeById`)
- **Variáveis**: camelCase (`employeeId`)
- **Constantes**: PascalCase (`MaxRetries`)
- **Interfaces**: Prefixo `I` (`IEmployeeRepository`)
- **Privados**: Prefixo `_` (`_repository`)

### Organização de Arquivos

- Um arquivo por classe
- Nome do arquivo = Nome da classe
- Agrupar por feature (não por tipo)

**✅ Bom**:
```
Features/
  Employees/
    Commands/
      CreateEmployee/
        CreateEmployeeCommand.cs
        CreateEmployeeCommandHandler.cs
        CreateEmployeeCommandValidator.cs
```

**❌ Ruim**:
```
Commands/
  CreateEmployeeCommand.cs
Handlers/
  CreateEmployeeCommandHandler.cs
Validators/
  CreateEmployeeCommandValidator.cs
```

## Adicionando Nova Feature

### 1. Criar Entidade (Domain)

```csharp
// Domain/Entities/Product.cs
public class Product : Entity
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    
    public static Result<Product> Create(string name, decimal price)
    {
        // Validações
        if (string.IsNullOrWhiteSpace(name))
            return Result<Product>.Failure(Error.Validation("Name", "Required"));
        
        var product = new Product { Name = name, Price = price };
        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id));
        
        return Result<Product>.Success(product);
    }
}
```

### 2. Criar Command (Application)

```csharp
// Application/Features/Products/Commands/CreateProduct/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    decimal Price
) : IRequest<Result<ProductResponse>>;

// CreateProductCommandValidator.cs
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// CreateProductCommandHandler.cs
public class CreateProductCommandHandler 
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var productResult = Product.Create(request.Name, request.Price);
        if (productResult.IsFailure)
            return Result<ProductResponse>.Failure(productResult.Error);
        
        await _repository.AddAsync(productResult.Value, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result<ProductResponse>.Success(new ProductResponse(...));
    }
}
```

### 3. Criar Controller (API)

```csharp
// Api/V1/Controllers/ProductsController.cs
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class ProductsController : MainController
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        var command = new CreateProductCommand(request.Name, request.Price);
        var result = await Sender.Send(command, ct);
        return HandleCreatedResult(result, nameof(GetById), p => new { id = p.Id });
    }
}
```

### 4. Configurar EF Core

```csharp
// Infrastructure/Persistence/Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
    }
}
```

### 5. Criar Migration

```bash
dotnet ef migrations add AddProduct \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

### 6. Aplicar Migration

```bash
dotnet ef database update \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

## Debugging

### Visual Studio

1. Abrir `EmployeeManagement.sln`
2. Definir `EmployeeManagement.Api` como projeto de inicialização
3. F5 para debug

### VS Code

1. Abrir pasta raiz
2. F5 → Selecionar ".NET Core Launch (web)"
3. Breakpoints funcionam normalmente

### Docker

```bash
# Ver logs em tempo real
docker logs -f employee-api

# Entrar no container
docker exec -it employee-api /bin/bash
```

## Testes

### Executar Todos

```bash
cd tests/EmployeeManagement.Tests
dotnet test
```

### Executar com Cobertura

```bash
dotnet test /p:CollectCoverage=true
```

### Executar Feature Específica

```bash
dotnet test --filter "FullyQualifiedName~CriarFuncionario"
```

### Watch Mode

```bash
dotnet watch test
```

## Migrations

### Criar

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

### Aplicar

```bash
dotnet ef database update \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

### Reverter

```bash
dotnet ef database update NomeMigrationAnterior \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

### Remover Última

```bash
dotnet ef migrations remove \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

### Gerar Script SQL

```bash
dotnet ef migrations script \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api \
  --output migration.sql
```

## Logs

### Visualizar Logs

**Console**: Logs aparecem automaticamente durante execução

**Docker**:
```bash
docker logs -f employee-api
```

### Níveis de Log

Configurar em `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

## Ferramentas Úteis

### dotnet CLI

```bash
# Listar projetos
dotnet sln list

# Adicionar projeto à solution
dotnet sln add src/NovoProject/NovoProject.csproj

# Limpar build
dotnet clean

# Publicar
dotnet publish -c Release
```

### EF Core Tools

```bash
# Instalar globalmente
dotnet tool install --global dotnet-ef

# Atualizar
dotnet tool update --global dotnet-ef

# Verificar versão
dotnet ef --version
```

## Troubleshooting Comum

### Erro: "Unable to connect to SQL Server"

1. Verificar se SQL Server está rodando
2. Verificar connection string
3. Verificar firewall

### Erro: "Certificate validation failed"

Adicionar `TrustServerCertificate=True` na connection string

### Erro: "Port already in use"

```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac
lsof -i :5000
kill -9 <PID>
```

## Próximos Passos

- [Arquitetura](02-ARQUITETURA.md)
- [API Reference](12-API-REFERENCE.md)
- [Testes](09-TESTES.md)
- [Troubleshooting](15-TROUBLESHOOTING.md)

