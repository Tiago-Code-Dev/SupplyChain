# Configuração e Variáveis de Ambiente

## appsettings.json

### Estrutura Completa

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EmployeeManagementDb;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyWithAtLeast32Characters!",
    "Issuer": "EmployeeManagement.Api",
    "Audience": "EmployeeManagement.Client",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "RateLimiting": {
    "EnableRateLimiting": true,
    "PermitLimit": 100,
    "WindowInSeconds": 60,
    "QueueLimit": 2,
    "LoginPermitLimit": 5,
    "LoginWindowInSeconds": 60
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:4200"]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

## Ambientes

### Development (appsettings.Development.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=EmployeeManagementDb;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Docker (appsettings.Docker.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver,1433;Database=EmployeeManagement;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Production

Use **variáveis de ambiente** ou **Azure Key Vault** para secrets.

## Seções de Configuração

### ConnectionStrings

| Chave | Descrição | Obrigatório |
|-------|-----------|-------------|
| `DefaultConnection` | SQL Server connection string | ✅ Sim |
| `Redis` | Redis connection string | ❌ Não (fallback para memory cache) |

### JWT

| Chave | Descrição | Padrão | Obrigatório |
|-------|-----------|--------|-------------|
| `Secret` | Chave secreta (mín. 32 chars) | - | ✅ Sim |
| `Issuer` | Emissor do token | `EmployeeManagement.Api` | ✅ Sim |
| `Audience` | Audiência do token | `EmployeeManagement.Client` | ✅ Sim |
| `AccessTokenExpirationMinutes` | Validade do access token | `15` | ❌ Não |
| `RefreshTokenExpirationDays` | Validade do refresh token | `7` | ❌ Não |

### RateLimiting

| Chave | Descrição | Padrão |
|-------|-----------|--------|
| `EnableRateLimiting` | Habilitar rate limiting | `true` |
| `PermitLimit` | Requisições permitidas (geral) | `100` |
| `WindowInSeconds` | Janela de tempo (geral) | `60` |
| `QueueLimit` | Fila de requisições | `2` |
| `LoginPermitLimit` | Requisições de login permitidas | `5` |
| `LoginWindowInSeconds` | Janela de tempo (login) | `60` |

### CORS

| Chave | Descrição | Exemplo |
|-------|-----------|---------|
| `AllowedOrigins` | Origens permitidas | `["http://localhost:3000"]` |

### Logging

| Nível | Descrição |
|-------|-----------|
| `Trace` | Informações muito detalhadas |
| `Debug` | Informações de debug |
| `Information` | Informações gerais |
| `Warning` | Avisos |
| `Error` | Erros |
| `Critical` | Erros críticos |

## Variáveis de Ambiente

### Sobrescrever Configurações

```bash
# Windows (PowerShell)
$env:ConnectionStrings__DefaultConnection="Server=..."
$env:Jwt__Secret="MySecret"

# Linux/Mac
export ConnectionStrings__DefaultConnection="Server=..."
export Jwt__Secret="MySecret"

# Docker Compose
environment:
  - ConnectionStrings__DefaultConnection=Server=...
  - Jwt__Secret=MySecret
```

### Hierarquia de Configuração

1. appsettings.json (base)
2. appsettings.{Environment}.json
3. User Secrets (Development)
4. Variáveis de Ambiente
5. Command Line Arguments

**Ordem de precedência**: Última sobrescreve anterior

## User Secrets (Development)

### Inicializar

```bash
cd src/EmployeeManagement/EmployeeManagement.Api
dotnet user-secrets init
```

### Adicionar Secret

```bash
dotnet user-secrets set "Jwt:Secret" "MyDevelopmentSecret"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=..."
```

### Listar Secrets

```bash
dotnet user-secrets list
```

### Remover Secret

```bash
dotnet user-secrets remove "Jwt:Secret"
```

## Azure Key Vault (Production)

### Configuração

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

### Nomear Secrets

Substitua `:` por `--`:
- `Jwt:Secret` → `Jwt--Secret`
- `ConnectionStrings:DefaultConnection` → `ConnectionStrings--DefaultConnection`

## AWS Secrets Manager

```csharp
builder.Configuration.AddSecretsManager(
    configurator: options =>
    {
        options.SecretFilter = entry => entry.Name.StartsWith("EmployeeManagement");
    });
```

## Configurações Tipadas

### Criar Classe

```csharp
public class JwtSettings
{
    public const string SectionName = "Jwt";
    
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
```

### Registrar

```csharp
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection(JwtSettings.SectionName));
```

### Usar

```csharp
public class JwtService
{
    private readonly JwtSettings _settings;
    
    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

## Validação de Configuração

```csharp
builder.Services.AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

public class JwtSettings
{
    [Required]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;
    
    [Required]
    public string Issuer { get; set; } = string.Empty;
    
    [Range(1, 60)]
    public int AccessTokenExpirationMinutes { get; set; } = 15;
}
```

## Boas Práticas

✅ Nunca commitar secrets no código  
✅ Usar User Secrets em Development  
✅ Usar Key Vault/Secrets Manager em Production  
✅ Validar configurações na inicialização  
✅ Usar configurações tipadas  
✅ Documentar todas as configurações  
✅ Usar valores padrão sensatos  
✅ Rotacionar secrets periodicamente  

## Próximos Passos

- [Docker e Deploy](10-DOCKER-DEPLOY.md)
- [Guia de Desenvolvimento](13-GUIA-DESENVOLVIMENTO.md)
- [Troubleshooting](15-TROUBLESHOOTING.md)

