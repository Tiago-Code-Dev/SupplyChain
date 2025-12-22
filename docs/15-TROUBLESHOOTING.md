# Troubleshooting

## Problemas Comuns

### Erro: "Unable to connect to SQL Server"

**Sintoma**: API não consegue conectar ao banco de dados.

**Soluções**:

1. **Verificar se SQL Server está rodando**:
```bash
docker ps | grep sqlserver
```

2. **Testar conexão manualmente**:
```bash
docker exec -it employee-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P SqlServer@123 -C -Q "SELECT 1"
```

3. **Verificar connection string**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver,1433;Database=EmployeeManagement;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True"
  }
}
```

4. **Adicionar `TrustServerCertificate=True`** se erro de certificado.

### Erro: "Certificate validation failed"

**Sintoma**: HTTPS não funciona, erro de certificado SSL.

**Soluções**:

1. **Regenerar certificado**:
```bash
# Windows
.\scripts\generate-dev-cert.ps1

# Linux/Mac
./scripts/generate-dev-cert.sh
```

2. **Usar apenas HTTP** (desenvolvimento):
```bash
docker-compose -f docker-compose.yml -f docker-compose.http-only.yml up
```

3. **Confiar no certificado** (Windows):
```powershell
dotnet dev-certs https --trust
```

### Erro: "Port 5000 is already in use"

**Sintoma**: Não consegue iniciar API, porta ocupada.

**Soluções**:

**Windows**:
```powershell
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

**Linux/Mac**:
```bash
lsof -i :5000
kill -9 <PID>
```

**Ou alterar porta** em `docker-compose.yml`:
```yaml
ports:
  - "5002:8080"  # Mudar de 5000 para 5002
```

### Erro: "401 Unauthorized"

**Sintoma**: Requisições retornam 401.

**Soluções**:

1. **Verificar se token está sendo enviado**:
```bash
curl -H "Authorization: Bearer {token}" http://localhost:5000/api/employees
```

2. **Verificar se token não expirou** (15 minutos de validade).

3. **Renovar token**:
```bash
curl -X POST http://localhost:5000/api/auth/refresh-token \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"..."}'
```

4. **Fazer login novamente**:
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@empresa.com","password":"Admin@123"}'
```

### Erro: "403 Forbidden"

**Sintoma**: Usuário autenticado mas sem permissão.

**Causa**: Tentando criar/editar funcionário com role superior ou igual.

**Solução**: Usar usuário com role superior:
- Employee (1) não pode criar ninguém
- Leader (2) pode criar apenas Employee (1)
- Director (3) pode criar Leader (2) e Employee (1)
- Admin (4) pode criar todos

### Erro: "409 Conflict - Email já cadastrado"

**Sintoma**: Não consegue criar funcionário, email duplicado.

**Soluções**:

1. **Usar email diferente**.

2. **Verificar funcionários existentes**:
```bash
curl -H "Authorization: Bearer {token}" \
  "http://localhost:5000/api/employees?filterByEmail=joao@empresa.com"
```

3. **Excluir funcionário existente** (se apropriado):
```bash
curl -X DELETE -H "Authorization: Bearer {token}" \
  http://localhost:5000/api/employees/{id}
```

### Erro: "429 Too Many Requests"

**Sintoma**: Rate limit excedido.

**Causa**: Muitas requisições em curto período.

**Soluções**:

1. **Aguardar 1 minuto** antes de tentar novamente.

2. **Desabilitar rate limiting** (apenas desenvolvimento):
```json
{
  "RateLimiting": {
    "EnableRateLimiting": false
  }
}
```

### Erro: "Migration pending"

**Sintoma**: Banco de dados desatualizado.

**Solução**:

```bash
dotnet ef database update \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

**Ou deixar a aplicação aplicar automaticamente** (já configurado no `Program.cs`).

### Erro: "Docker container exits immediately"

**Sintoma**: Container inicia e para imediatamente.

**Soluções**:

1. **Ver logs**:
```bash
docker logs employee-api
```

2. **Verificar se SQL Server está healthy**:
```bash
docker ps
```

3. **Aguardar SQL Server inicializar** (pode levar 30-60 segundos na primeira vez).

4. **Verificar health check**:
```bash
docker inspect employee-sqlserver | grep -A 10 Health
```

### Erro: "Cannot access a disposed object"

**Sintoma**: Erro ao acessar DbContext.

**Causa**: DbContext com escopo incorreto.

**Solução**: Garantir que DbContext é `Scoped`:
```csharp
services.AddDbContext<AppDbContext>(options => ..., ServiceLifetime.Scoped);
```

### Erro: "Sequence contains no elements"

**Sintoma**: `FirstAsync()` ou `SingleAsync()` não encontra resultado.

**Solução**: Usar `FirstOrDefaultAsync()` ou `SingleOrDefaultAsync()`:
```csharp
var employee = await _context.Employees
    .FirstOrDefaultAsync(e => e.Id == id);

if (employee == null)
    return Result.Failure(Error.NotFound("Employee", id));
```

### Erro: "The instance of entity type cannot be tracked"

**Sintoma**: EF Core tenta rastrear entidade duplicada.

**Solução**: Usar `AsNoTracking()` para queries:
```csharp
var employee = await _context.Employees
    .AsNoTracking()
    .FirstOrDefaultAsync(e => e.Id == id);
```

## Logs e Diagnóstico

### Ver Logs da API

```bash
# Docker
docker logs -f employee-api

# Local
# Logs aparecem no console
```

### Ver Logs do SQL Server

```bash
docker logs -f employee-sqlserver
```

### Aumentar Nível de Log

Em `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

### Ver Queries SQL

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

## Health Checks

### Verificar Saúde da API

```bash
curl http://localhost:5000/health
```

**Response esperada**:
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "duration": 45.2
    }
  ]
}
```

## Limpar e Reiniciar

### Limpar Docker

```bash
# Parar e remover containers
docker-compose down

# Remover volumes (APAGA DADOS!)
docker-compose down -v

# Limpar imagens
docker system prune -a

# Reconstruir tudo
docker-compose up --build
```

### Limpar Build

```bash
dotnet clean
dotnet build
```

### Resetar Banco de Dados

```bash
# Remover banco
dotnet ef database drop --force \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api

# Recriar
dotnet ef database update \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api
```

## FAQ

### Como alterar a senha padrão do SQL Server?

1. Alterar em `docker-compose.yml`:
```yaml
environment:
  - MSSQL_SA_PASSWORD=NovaSenha@123
```

2. Alterar connection string:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...Password=NovaSenha@123..."
  }
}
```

### Como adicionar novo usuário de teste?

Editar `Infrastructure/Identity/IdentitySeeder.cs` e adicionar:
```csharp
await CreateUserIfNotExists("novo@empresa.com", "Senha@123", "Novo", "Usuario", "Employee");
```

### Como desabilitar HTTPS?

Usar `docker-compose.http-only.yml`:
```bash
docker-compose -f docker-compose.yml -f docker-compose.http-only.yml up
```

### Como conectar ao SQL Server do host?

```bash
# Connection string do host
Server=localhost,1433;Database=EmployeeManagement;User Id=sa;Password=SqlServer@123;TrustServerCertificate=True
```

## Suporte

### Documentação

- [Visão Geral](01-VISAO-GERAL.md)
- [Guia de Desenvolvimento](13-GUIA-DESENVOLVIMENTO.md)
- [API Reference](12-API-REFERENCE.md)

### Logs

Sempre incluir logs ao reportar problemas:
```bash
docker logs employee-api > api-logs.txt
docker logs employee-sqlserver > sql-logs.txt
```

### Issues

Ao abrir issue, incluir:
- Versão do .NET
- Sistema operacional
- Logs relevantes
- Passos para reproduzir

