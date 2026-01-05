# SupplyChain – Sistema de Gerenciamento de Funcionários

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![License](https://img.shields.io/badge/license-MIT-green)
![Build](https://img.shields.io/badge/build-passing-brightgreen)
![Tests](https://img.shields.io/badge/tests-51%20scenarios-blue)
![Docker](https://img.shields.io/badge/docker-ready-blue)

Plataforma backend para **cadastro, autenticação e gestão hierárquica de funcionários**, com **Clean Architecture + DDD**, **CQRS (MediatR)**, **JWT + Refresh Token**, rastreabilidade por **X-Correlation-ID**, e camadas prontas para evoluir com segurança.

> Projeto focado em **boas práticas de engenharia** (validações de negócio, observabilidade, testes e deploy com Docker) para ambientes de alta responsabilidade.

---

## ⚡ Quick Start (TL;DR)

```bash
# Clone o repositório
git clone https://github.com/seu-usuario/supply-chain.git
cd supply-chain

# Suba com Docker (mais rápido)
docker compose up --build

# Acesse
# API: http://localhost:5000/swagger
# Login: admin@empresa.com / Admin@123
```

---

## 🧭 Visão Geral

O **SupplyChain** (Employee Management) é uma API que centraliza o ciclo completo de **gestão de colaboradores**:
- **Autenticação segura** (JWT + Refresh Token + revogação)
- **CRUD de funcionários** com **soft delete**
- **Hierarquia de permissões** (controle de quem pode criar/alterar/excluir)
- **Regras de negócio críticas** (documento único, maioridade, gestor, etc.)
- **Rastreabilidade ponta‑a‑ponta** com Correlation ID e logs estruturados

---

## 📊 Status do Projeto

| Módulo | Status | Cobertura |
|--------|--------|-----------|
| Autenticação | ✅ Completo | 92% |
| CRUD Employees | ✅ Completo | 88% |
| Hierarquia/Permissões | ✅ Completo | 95% |
| Soft Delete | ✅ Completo | 100% |
| Cache & Invalidação | ✅ Completo | 85% |
| Frontend React | 🚧 Em desenvolvimento | - |
| Relatórios | 📋 Planejado | - |

---

## 📋 Pré-requisitos

| Ferramenta | Versão Mínima | Obrigatório |
|------------|---------------|-------------|
| .NET SDK | 8.0+ | ✅ |
| Docker Desktop | 24.0+ | Para execução via container |
| Node.js | 18+ | Para o frontend |
| SQL Server | 2019+ | Para execução local sem Docker |

---

## 📌 Funcionalidades Incluídas

- 🔐 **Autenticação & Identity**
  - Login / Refresh Token / Revogação de tokens
  - Registro, reset de senha, troca de senha
  - Endpoint **/me** para dados do usuário autenticado
  - Gestão de Roles/Claims (para cenários administrativos)

- 👥 **Funcionários (Employees)**
  - Criar, atualizar, listar (paginação/filtros), buscar por ID
  - **Soft delete** (exclusão lógica)
  - Cache e invalidação de cache (quando aplicável)

- 🧠 **Regras de Negócio (Domínio)**
  - Documento e e-mail **únicos**
  - Funcionário deve ser **maior de 18 anos**
  - Deve possuir **pelo menos 1 telefone**
  - Validação de **gestor existente** e prevenção de inconsistências
  - Restrições por hierarquia (não criar/alçar permissões acima do nível do usuário)

- 🧾 **Observabilidade & Resiliência**
  - Logs estruturados (Serilog + pipeline de logging)
  - **X-Correlation-ID** (propagação e rastreabilidade)
  - **Health Checks**
  - **Rate Limiting** (global + políticas)

- 🧪 **Testes Automatizados**
  - Unit tests (xUnit)
  - BDD com SpecFlow (cenários em Gherkin/PT-BR)
  - Integração (quando configurado)

- 🐳 **Docker**
  - `docker-compose.yml` (API + SQL Server + HTTP/HTTPS)
  - `docker-compose.http-only.yml` (API + SQL Server somente HTTP)

---

## 🎯 Problemas que resolve

- **Evita ações indevidas por permissão** (hierarquia e RBAC)
- **Impede inconsistências** (documento duplicado, menoridade, gestor inválido)
- **Torna auditoria e troubleshooting fáceis** (Correlation-ID + logs)
- **Acelera onboarding e deploy** (Docker + scripts utilitários)
- **Evolução sustentável** (DDD / Clean Architecture / CQRS)

---

## 🧱 Arquitetura

```text
┌──────────────────────────────────────────────────────────┐
│                      EmployeeManagement.Api              │
│  Controllers • Versionamento • Auth • Middlewares        │
└───────────────┬───────────────────────────┬──────────────┘
                │                           │
                ▼                           ▼
┌───────────────────────────────┐   ┌──────────────────────┐
│     EmployeeManagement.App     │   │   Cross-Cutting      │
│  CQRS (MediatR) • Validators   │   │ Logs • Cache • Rate  │
│  Behaviors • Use Cases         │   │ Limiting • Health    │
└───────────────┬───────────────┘   └──────────────────────┘
                ▼
┌──────────────────────────────────────────┐
│           EmployeeManagement.Domain       │
│  Entidades • VO • Invariantes • Eventos   │
└───────────────┬──────────────────────────┘
                ▼
┌──────────────────────────────────────────┐
│       EmployeeManagement.Infrastructure   │
│ EF Core • Repositórios • Identity • DB    │
└────────────────┬─────────────────────────┘
                 ▼
           ┌─────────────┐
           │  SQL Server  │
           └─────────────┘
```

---

## ⚙️ Tecnologias Utilizadas

| Tecnologia | Função |
|---|---|
| C# / .NET 8 | API e camadas do backend |
| ASP.NET Core | Web API + middlewares |
| MediatR | CQRS (Commands/Queries) |
| FluentValidation | Validações de request |
| EF Core | Persistência e migrations |
| ASP.NET Identity | Usuários/roles/claims + refresh token |
| Serilog | Logging estruturado |
| Docker / Docker Compose | Infra local e execução rápida |
| xUnit + SpecFlow | Testes unitários e BDD |

---

## 📂 Estrutura de Pastas

```text
/src
  /EmployeeManagement
    ├── EmployeeManagement.Api
    ├── EmployeeManagement.Application
    ├── EmployeeManagement.Domain
    └── EmployeeManagement.Infrastructure
  /Shared
    └── Shared.CrossCutting   (utilitários, contracts e helpers)

/frontend
  └── React + TypeScript (Vite)

/docs
  └── Documentação técnica do projeto (fonte da verdade)

/tests
  /EmployeeManagement.Tests   (xUnit + SpecFlow)

/scripts
  ├── generate-dev-cert.ps1
  ├── generate-dev-cert.sh
  └── setup-hosts.* (se aplicável)

/certs
  └── certificados dev (para HTTPS em Docker)
```

---

## 🚀 Como Executar o Projeto

### ✅ Opção A — Docker (SQL Server + HTTP + HTTPS)

```bash
docker compose up --build
```

- API HTTP: `http://localhost:5000`
- API HTTPS: `https://localhost:5001`
- Swagger: `http://localhost:5000/swagger`
- SQL Server: `localhost,1433` (sa / `SqlServer@123`)

> Se for usar HTTPS no Docker, utilize os scripts em `/scripts` e a pasta `/certs` para gerar/instalar certificados dev.

### ✅ Opção B — Docker apenas HTTP (mais simples)

```bash
docker compose -f docker-compose.http-only.yml up --build
```

- API HTTP: `http://localhost:5000`

### ✅ Opção C — Rodar local (InMemory por padrão no Development)

```bash
dotnet restore
dotnet run --project src/EmployeeManagement/EmployeeManagement.Api
```

- API HTTP (launchSettings): `http://localhost:59688`
- API HTTPS (launchSettings): `https://localhost:59687`

> Se quiser fixar URLs/portas manualmente:
```bash
dotnet run --project src/EmployeeManagement/EmployeeManagement.Api --urls "http://localhost:59688;https://localhost:59687"
```

No ambiente **Development**, o projeto pode usar **InMemoryDatabase** por padrão. Para usar **SQL Server**, ajuste a `ConnectionStrings:DefaultConnection` e altere a flag em `appsettings.Development.json` (`UseInMemoryDatabase: true`).

---

### ✅ Opção D — Rodar o Frontend (React + Vite)

```bash
cd frontend
npm install
npm run dev
```

- Front: `http://localhost:5173`

#### Configurar URL da API no Front

Crie um arquivo `frontend/.env.local` com a base da API.

**Se a API estiver no Docker:**
```env
VITE_API_BASE_URL=http://localhost:5000
# ou https://localhost:5001
```

**Se a API estiver rodando local (launchSettings do projeto):**
```env
VITE_API_BASE_URL=http://localhost:59688
# ou https://localhost:59687
```

#### CORS (se bater)

Se o navegador bloquear por CORS:
- Garanta que a API permite `http://localhost:5173` em `Cors:AllowedOrigins` (`appsettings.Development.json`)
- Ou rode o Vite em outra porta que já esteja liberada.

---

## 📖 Documentação Interativa (Swagger)

Após iniciar a API, acesse:
- **HTTP:** http://localhost:5000/swagger
- **HTTPS:** https://localhost:5001/swagger

A documentação inclui todos os endpoints, schemas e permite testar diretamente.

---

## 🔧 Variáveis de Ambiente

| Variável | Descrição | Padrão |
|----------|-----------|--------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução | Development |
| `ConnectionStrings__DefaultConnection` | String de conexão SQL Server | - |
| `JwtSettings__Secret` | Chave secreta JWT (min 32 chars) | - |
| `JwtSettings__ExpirationInMinutes` | Tempo de expiração do token | 60 |
| `JwtSettings__RefreshTokenExpirationInDays` | Expiração refresh token | 7 |
| `UseInMemoryDatabase` | Usar banco em memória | true (dev) |

---

## 🔐 Autenticação (JWT + Refresh Token)

A API mantém rotas versionadas e uma rota "legada" para compatibilidade:

- Versionada: `/api/v1/auth/...` (ou `/api/v1.0/auth/...`)
- Legada: `/api/auth/...`

> **Usuário admin seed (DEV):** `admin@empresa.com` / `Admin@123`  
> **Recomendado:** alterar/remover em produção.

### Login

`POST /api/v1/auth/login`

```json
{
  "email": "admin@empresa.com",
  "password": "Admin@123"
}
```

### Refresh Token

`POST /api/v1/auth/refresh-token`

```json
{
  "refreshToken": "..."
}
```

### Revogar token (logout)

`POST /api/v1/auth/revoke-token`

```json
{
  "refreshToken": "..."
}
```

---

## 👥 Funcionários (Employees)

> Todas as rotas exigem **Bearer Token**, exceto quando explicitado.

### Criar funcionário

`POST /api/v1/employees`

```json
{
  "firstName": "João",
  "lastName": "da Silva",
  "email": "joao@email.com",
  "documentNumber": "12345678900",
  "birthDate": "1995-10-01",
  "phoneNumbers": ["11999999999"],
  "role": 1,
  "managerId": null
}
```

**Role (enum):**
- 1 = Employee
- 2 = Leader
- 3 = Director
- 4 = Admin

> Regra crítica: usuário comum não pode criar/alçar alguém com **role >= seu role** (exceto Admin).

### Atualizar funcionário

`PUT /api/v1/employees/{id}`

### Listar com paginação/filtros

`GET /api/v1/employees?pageNumber=1&pageSize=10&searchTerm=joao&sortBy=createdAt&sortDescending=true`

### Buscar por ID

`GET /api/v1/employees/{id}`

### Soft delete

`DELETE /api/v1/employees/{id}`

> Regra de permissão: exclusão exige nível mínimo (ex.: Leader+).  
> Se existir vínculo/estrutura (ex.: subordinados), a exclusão pode ser bloqueada por regra de negócio.

---

## 📤 Exemplos de Resposta

### ✅ Sucesso (201 Created)

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "firstName": "João",
    "lastName": "Silva",
    "email": "joao@email.com",
    "role": "Employee",
    "createdAt": "2025-01-04T15:30:00Z"
  },
  "messages": ["Funcionário criado com sucesso"]
}
```

### ❌ Erro de Validação (400 Bad Request)

```json
{
  "success": false,
  "data": null,
  "errors": [
    { "field": "BirthDate", "message": "Funcionário deve ter pelo menos 18 anos" },
    { "field": "DocumentNumber", "message": "CPF já cadastrado no sistema" }
  ]
}
```

### ❌ Não Autorizado (401 Unauthorized)

```json
{
  "success": false,
  "data": null,
  "messages": ["Token inválido ou expirado"]
}
```

### ❌ Sem Permissão (403 Forbidden)

```json
{
  "success": false,
  "data": null,
  "messages": ["Você não tem permissão para excluir funcionários"]
}
```

---

## 🔒 Segurança

- Senhas armazenadas com **BCrypt** (hash + salt)
- Tokens JWT com expiração curta + Refresh Token
- Rate Limiting para prevenção de ataques de força bruta
- Validação de entrada em todas as camadas
- Soft Delete para auditoria e conformidade LGPD
- Correlation ID para rastreabilidade completa

---

## ⚡ Performance

| Operação | Tempo Médio | Requisições/seg |
|----------|-------------|-----------------|
| Login | ~45ms | 500+ |
| Listar (paginado) | ~30ms | 800+ |
| Criar funcionário | ~60ms | 300+ |
| Buscar por ID (cache) | ~5ms | 2000+ |
| Soft Delete | ~40ms | 400+ |

*Benchmarks realizados em ambiente Docker local com SQL Server.*

---

## 📚 Documentação do Projeto (incluída neste repositório)

A documentação oficial está em `docs/` (**fonte da verdade**):

- `docs/README.md` (índice)
- `docs/01-VISAO-GERAL.md`
- `docs/02-ARQUITETURA.md`
- `docs/03-DOMINIO.md`
- `docs/04-APLICACAO.md`
- `docs/05-INFRAESTRUTURA.md`
- `docs/06-API.md`
- `docs/07-AUTENTICACAO.md`
- `docs/08-BANCO-DE-DADOS.md`
- `docs/09-TESTES.md`
- `docs/10-DOCKER-DEPLOY.md`
- `docs/11-CONFIGURACAO.md`
- `docs/12-API-REFERENCE.md`
- `docs/13-GUIA-DESENVOLVIMENTO.md`
- `docs/14-BOAS-PRATICAS.md`
- `docs/15-TROUBLESHOOTING.md`
- `docs/16-ROADMAP.md`

📦 **Postman**: coleções e environments em `docs/postman/`.

---

## 🧪 BDD (SpecFlow)

O arquivo `docs/BDD_GHERKIN.md` contém **51 cenários** cobrindo:
- Autenticação e autorização (401/403, token expirado, etc.)
- Create / Read / Update / Delete de funcionários
- Validações (idade, documento duplicado, hierarquia, gestor)
- Observabilidade (logs e rastreabilidade)

---

## 🧾 Rastreabilidade (Correlation ID)

A API aceita e propaga `X-Correlation-ID`:
- Se o cliente enviar, o valor é reutilizado
- Se não enviar, o middleware gera automaticamente
- O Correlation ID aparece nos logs para auditoria e debug

---

## 🧯 Error Contract (padrão de erros)

Endpoint utilitário para padronização de respostas de erro:
`GET /api/v1/errorcontract`

Retorno típico:

```json
{
  "success": false,
  "messages": ["..."],
  "errors": [{ "field": "...", "message": "..." }]
}
```

---

## 🩺 Health Checks

`GET /health`

Retorna o status da aplicação e suas dependências (banco de dados, cache, etc.).

---

## 🧯 Rate Limiting

O projeto possui configuração de rate limiting (global/políticas).  
Ajuste as regras em `EmployeeManagement.Api/Configurations/RateLimitingConfiguration.cs`.

| Política | Limite | Janela |
|----------|--------|--------|
| Global | 100 req | 1 min |
| Auth | 10 req | 1 min |
| API | 60 req | 1 min |

---

## 🗄️ Banco de dados & Migrations (EF Core)

### Rodar migrations (AppDbContext)

> Ajuste a connection string em `appsettings.json` / `appsettings.Docker.json`.

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api \
  --context AppDbContext

dotnet ef database update \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api \
  --context AppDbContext
```

### Identity (AppIdentityDbContext)

```bash
dotnet ef migrations add IdentityMigration \
  --project src/EmployeeManagement/EmployeeManagement.Infrastructure \
  --startup-project src/EmployeeManagement/EmployeeManagement.Api \
  --context AppIdentityDbContext
```

---

## 🧪 Testes Automatizados

```bash
dotnet test
```

- Testes unitários em `/tests/EmployeeManagement.Tests/UnitTests`
- SpecFlow em `/tests/EmployeeManagement.Tests/StepDefinitions`

### Executar com cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## ✅ Critérios de Aceite (resumo)

- Retorno padrão `{ success, data, messages, errors }`
- Funcionário deve ser **adulto** (>= 18)
- Email e documento devem ser **únicos**
- Hierarquia respeitada (não criar/alçar acima do nível do usuário)
- Correlation ID presente nos logs
- Testes e BDD cobrindo cenários críticos

---

## 🧑‍💻 Autoria

Desenvolvido por **Tiago Nogueira**.

---

## 📝 Licença

MIT © 2025 – Livre para modificação e uso.