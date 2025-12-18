# Employee Management API

Sistema de Gerenciamento de Funcionários desenvolvido com **.NET 8**, seguindo princípios de **Clean Architecture**, **DDD**, **SOLID** e **CQRS**.

## ?? Início Rápido

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- PowerShell (Windows) ou Bash (Linux/Mac)

### Executando com Docker (HTTP + HTTPS)

#### 1. Gerar Certificado de Desenvolvimento

**Windows (PowerShell):**
```powershell
.\scripts\generate-dev-cert.ps1
```

**Linux/Mac (Bash):**
```bash
chmod +x ./scripts/generate-dev-cert.sh
./scripts/generate-dev-cert.sh
```

#### 2. Iniciar os Containers

```bash
docker-compose up --build
```

#### 3. Acessar a API

| Protocolo | URL | Descrição |
|-----------|-----|-----------|
| HTTP | http://localhost:5000 | Acesso sem SSL |
| HTTPS | https://localhost:5001 | Acesso com SSL |
| Swagger | http://localhost:5000/swagger | Documentação da API |
| Health Check | http://localhost:5000/health | Status da aplicação |

### Executando apenas com HTTP (Desenvolvimento Simplificado)

Se você não precisa de HTTPS durante o desenvolvimento:

```bash
docker-compose -f docker-compose.yml -f docker-compose.http-only.yml up --build
```

### Executando Localmente (sem Docker)

```bash
cd src/EmployeeManagement/EmployeeManagement.Api
dotnet run
```

Acesse: https://localhost:5051/swagger

## ?? Estrutura do Projeto

```
??? src/
?   ??? EmployeeManagement/
?   ?   ??? EmployeeManagement.Api/          # Camada de apresentação
?   ?   ??? EmployeeManagement.Application/  # Casos de uso e handlers
?   ?   ??? EmployeeManagement.Domain/       # Entidades e regras de negócio
?   ?   ??? EmployeeManagement.Infrastructure/ # Persistência e serviços externos
?   ??? Shared/
?       ??? Shared.Contracts/                # DTOs e contratos
?       ??? Shared.CrossCutting/             # Utilitários compartilhados
??? tests/
?   ??? EmployeeManagement.Tests/            # Testes unitários e integração
??? scripts/
?   ??? generate-dev-cert.ps1                # Script Windows para certificados
?   ??? generate-dev-cert.sh                 # Script Linux/Mac para certificados
??? certs/                                   # Certificados (gerado automaticamente)
??? docker-compose.yml                       # Orquestração Docker (HTTP + HTTPS)
??? docker-compose.http-only.yml             # Override para apenas HTTP
??? README.md
```

## ?? Autenticação

A API utiliza **JWT (JSON Web Token)** para autenticação.

### Níveis de Permissão

| Nível | Permissões |
|-------|------------|
| Employee | Leitura |
| Leader | Leitura + Criação/Edição de Employees |
| Director | Acesso total |

### Obtendo Token

```bash
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@supply.com",
  "password": "Admin@123456"
}
```

## ?? Docker

### Serviços

| Serviço | Container | Porta | Descrição |
|---------|-----------|-------|-----------|
| API | employee-api | 5000 (HTTP), 5001 (HTTPS) | Aplicação .NET 8 |
| SQL Server | employee-sqlserver | 1433 | Banco de dados |

### Comandos Úteis

```bash
# Iniciar containers
docker-compose up -d

# Reconstruir containers
docker-compose up --build -d

# Ver logs da API
docker logs -f employee-api

# Parar containers
docker-compose down

# Limpar volumes (apaga dados)
docker-compose down -v
```

### Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| ASPNETCORE_ENVIRONMENT | Ambiente de execução | Docker |
| ConnectionStrings__DefaultConnection | String de conexão SQL Server | - |
| Jwt__Secret | Chave secreta JWT | - |
| Jwt__AccessTokenExpirationMinutes | Expiração do access token | 15 |
| Jwt__RefreshTokenExpirationDays | Expiração do refresh token | 7 |

## ?? Testes

```bash
cd tests/EmployeeManagement.Tests
dotnet test
```

## ?? Licença

Este projeto está sob a licença MIT.