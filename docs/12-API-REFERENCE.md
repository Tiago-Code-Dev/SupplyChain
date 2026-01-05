# API Reference

## Base URL

- **Development**: `http://localhost:5000` ou `https://localhost:5001`
- **Docker**: `http://localhost:5000` ou `https://localhost:5001`
- **Production**: `https://api.seudominio.com`

## Autenticação

Todos os endpoints (exceto `/api/auth/login` e `/api/auth/refresh-token`) requerem autenticação JWT.

**Header**:
```
Authorization: Bearer {access_token}
```

## Endpoints de Autenticação

### POST /api/auth/login

Autentica usuário e retorna tokens.

**Request**:
```json
{
  "email": "admin@empresa.com",
  "password": "Admin@123"
}
```

**Response** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "accessTokenExpiresAt": "2025-12-21T15:30:00Z",
  "refreshToken": "CfDJ8KtcOY3kM...",
  "refreshTokenExpiresAt": "2025-12-28T14:30:00Z",
  "user": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "admin@empresa.com",
    "fullName": "Admin User",
    "roles": ["Admin"]
  }
}
```

**Erros**:
- `401 Unauthorized`: Credenciais inválidas
- `429 Too Many Requests`: Muitas tentativas de login

### POST /api/auth/refresh-token

Renova access token usando refresh token.

**Request**:
```json
{
  "refreshToken": "CfDJ8KtcOY3kM..."
}
```

**Response**: Mesmo formato do login

### POST /api/auth/change-password

Altera senha do usuário autenticado.

**Headers**: `Authorization: Bearer {token}`

**Request**:
```json
{
  "currentPassword": "Admin@123",
  "newPassword": "NewAdmin@456"
}
```

**Response**: `204 No Content`

**Erros**:
- `400 Bad Request`: Senha atual incorreta ou nova senha inválida
- `401 Unauthorized`: Token inválido

### GET /api/auth/me

Retorna dados do usuário autenticado.

**Headers**: `Authorization: Bearer {token}`

**Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "admin@empresa.com",
  "firstName": "Admin",
  "lastName": "User",
  "fullName": "Admin User",
  "employeeId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "isActive": true,
  "roles": ["Admin"],
  "claims": {}
}
```

## Endpoints de Roles

### GET /api/v1/roles

Lista todos os roles (sistema + customizados).

**Headers**: `Authorization: Bearer {token}`

**Autorização**: Requer `Admin`

**Response** (200 OK):
```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "name": "Employee",
    "displayName": "Funcionário",
    "hierarchyLevel": 10,
    "isSystemRole": true
  },
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "name": "Leader",
    "displayName": "Líder",
    "hierarchyLevel": 20,
    "isSystemRole": true
  },
  {
    "id": "33333333-3333-3333-3333-333333333333",
    "name": "Director",
    "displayName": "Diretor",
    "hierarchyLevel": 30,
    "isSystemRole": true
  },
  {
    "id": "44444444-4444-4444-4444-444444444444",
    "name": "Admin",
    "displayName": "Administrador",
    "hierarchyLevel": 100,
    "isSystemRole": true
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Supervisor",
    "displayName": "Supervisor de Área",
    "hierarchyLevel": 15,
    "isSystemRole": false
  }
]
```

**Erros**:
- `401 Unauthorized`: Token inválido
- `403 Forbidden`: Usuário não é Admin

### GET /api/v1/roles/{id}

Obtém um role específico por ID.

**Headers**: `Authorization: Bearer {token}`

**Autorização**: Requer `Admin`

**Response** (200 OK):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Supervisor",
  "displayName": "Supervisor de Área",
  "hierarchyLevel": 15,
  "isSystemRole": false
}
```

**Erros**:
- `404 Not Found`: Role não encontrado
- `403 Forbidden`: Usuário não é Admin

### POST /api/v1/roles

Cria um novo role customizado.

**Headers**: `Authorization: Bearer {token}`

**Autorização**: Requer `Admin`

**Request**:
```json
{
  "name": "Supervisor",
  "displayName": "Supervisor de Área",
  "hierarchyLevel": 15
}
```

**Validações**:
- `name`: Obrigatório, único, apenas letras e números
- `displayName`: Obrigatório, mínimo 2 caracteres
- `hierarchyLevel`: Entre 1 e 100

**Response** (201 Created):
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Supervisor",
  "displayName": "Supervisor de Área",
  "hierarchyLevel": 15,
  "isSystemRole": false
}
```

**Headers de Response**:
```
Location: /api/v1/roles/550e8400-e29b-41d4-a716-446655440000
```

**Erros**:
- `400 Bad Request`: Dados inválidos
- `403 Forbidden`: Usuário não é Admin
- `409 Conflict`: Nome já existe

**Exemplo de Erro** (409):
```json
{
  "error": "CustomRole.Conflict",
  "message": "Role with name 'Supervisor' already exists"
}
```

### PUT /api/v1/roles/{id}

Atualiza um role customizado (apenas `displayName` e `hierarchyLevel`).

**Headers**: `Authorization: Bearer {token}`

**Autorização**: Requer `Admin`

**Request**:
```json
{
  "displayName": "Supervisor Sênior",
  "hierarchyLevel": 18
}
```

**Response**: `204 No Content`

**Erros**:
- `400 Bad Request`: Dados inválidos
- `403 Forbidden`: Tentativa de editar role do sistema ou usuário não é Admin
- `404 Not Found`: Role não encontrado

**Exemplo de Erro** (403):
```json
{
  "error": "SystemRole.Validation",
  "message": "Cannot modify system roles"
}
```

### DELETE /api/v1/roles/{id}

Remove um role customizado.

**Headers**: `Authorization: Bearer {token}`

**Autorização**: Requer `Admin`

**Response**: `204 No Content`

**Erros**:
- `403 Forbidden`: Tentativa de deletar role do sistema ou usuário não é Admin
- `404 Not Found`: Role não encontrado

> ⚠️ **Atenção**: Não há validação se o role está sendo usado por funcionários. Verifique antes de deletar.

## Endpoints de Funcionários

### GET /api/employees

Lista funcionários com paginação e filtros.

**Headers**: `Authorization: Bearer {token}`

**Query Parameters**:
| Parâmetro | Tipo | Descrição | Padrão |
|-----------|------|-----------|--------|
| `pageNumber` | int | Número da página | 1 |
| `pageSize` | int | Itens por página (máx: 50) | 10 |
| `searchTerm` | string | Busca em nome, email, documento | - |
| `filterByName` | string | Filtro por nome | - |
| `filterByEmail` | string | Filtro por email | - |
| `filterByRole` | int | Filtro por role (1-4) | - |
| `filterByManagerId` | guid | Filtro por gestor | - |
| `sortBy` | string | Campo de ordenação | createdAt |
| `sortDescending` | bool | Ordem decrescente | false |

**Exemplo**:
```
GET /api/employees?pageNumber=1&pageSize=20&searchTerm=silva&sortBy=firstname
```

**Response** (200 OK):
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "firstName": "João",
      "lastName": "Silva",
      "email": "joao.silva@empresa.com",
      "documentNumber": "12345678900",
      "birthDate": "1990-01-15T00:00:00Z",
      "role": 1,
      "managerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "phoneNumbers": ["11999999999", "11888888888"]
    }
  ],
  "totalCount": 45,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

### GET /api/employees/{id}

Busca funcionário por ID.

**Headers**: `Authorization: Bearer {token}`

**Response** (200 OK):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "João",
  "lastName": "Silva",
  "email": "joao.silva@empresa.com",
  "documentNumber": "12345678900",
  "birthDate": "1990-01-15T00:00:00Z",
  "role": 1,
  "managerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "phoneNumbers": ["11999999999"]
}
```

**Erros**:
- `404 Not Found`: Funcionário não encontrado

### POST /api/employees

Cria novo funcionário.

**Headers**: `Authorization: Bearer {token}`

**Request**:
```json
{
  "firstName": "João",
  "lastName": "Silva",
  "email": "joao.silva@empresa.com",
  "documentNumber": "12345678900",
  "birthDate": "1990-01-15",
  "password": "Senha@123456",
  "role": 1,
  "managerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "phoneNumbers": ["11999999999", "11888888888"]
}
```

**Response** (201 Created):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "firstName": "João",
  "lastName": "Silva",
  "email": "joao.silva@empresa.com",
  "documentNumber": "12345678900",
  "birthDate": "1990-01-15T00:00:00Z",
  "role": 1,
  "managerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "phoneNumbers": ["11999999999", "11888888888"]
}
```

**Headers de Response**:
```
Location: /api/employees/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Erros**:
- `400 Bad Request`: Dados inválidos
- `403 Forbidden`: Sem permissão para criar com esta role
- `409 Conflict`: Email ou documento já existente

### PUT /api/employees/{id}

Atualiza funcionário existente.

**Headers**: `Authorization: Bearer {token}`

**Request**:
```json
{
  "firstName": "João",
  "lastName": "Silva Santos",
  "email": "joao.silva@empresa.com",
  "birthDate": "1990-01-15",
  "role": 2,
  "managerId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "phoneNumbers": ["11999999999"]
}
```

**Response** (200 OK): Mesmo formato do GET

**Erros**:
- `400 Bad Request`: Dados inválidos
- `403 Forbidden`: Sem permissão
- `404 Not Found`: Funcionário não encontrado
- `409 Conflict`: Email já utilizado

### DELETE /api/employees/{id}

Exclui funcionário (soft delete).

**Headers**: `Authorization: Bearer {token}`

**Response**: `204 No Content`

**Erros**:
- `403 Forbidden`: Sem permissão
- `404 Not Found`: Funcionário não encontrado

## Códigos de Status HTTP

| Código | Descrição |
|--------|-----------|
| `200 OK` | Sucesso |
| `201 Created` | Recurso criado |
| `204 No Content` | Sucesso sem conteúdo |
| `400 Bad Request` | Dados inválidos |
| `401 Unauthorized` | Não autenticado |
| `403 Forbidden` | Sem permissão |
| `404 Not Found` | Recurso não encontrado |
| `409 Conflict` | Conflito (duplicação) |
| `429 Too Many Requests` | Rate limit excedido |
| `500 Internal Server Error` | Erro interno |

## Formato de Erros

```json
{
  "error": "Validation.Error",
  "message": "O nome é obrigatório; Email inválido"
}
```

## Rate Limiting

### Política Geral
- **Limite**: 100 requisições/minuto
- **Header de resposta**: `X-RateLimit-Remaining`

### Política de Login
- **Limite**: 5 requisições/minuto
- **Proteção**: Contra brute force

**Response quando excedido** (429):
```json
{
  "error": "Too many requests. Please try again later."
}
```

## Versionamento

A API suporta versionamento via URL:

```
GET /api/v1/employees
GET /api/v2/employees
```

Versão padrão: `v1`

## Exemplos com cURL

### Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@empresa.com","password":"Admin@123"}'
```

### Listar Roles

```bash
curl -X GET http://localhost:5000/api/v1/roles \
  -H "Authorization: Bearer {token}"
```

### Criar Role Customizado

```bash
curl -X POST http://localhost:5000/api/v1/roles \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Supervisor",
    "displayName": "Supervisor de Área",
    "hierarchyLevel": 15
  }'
```

### Atualizar Role

```bash
curl -X PUT http://localhost:5000/api/v1/roles/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "displayName": "Supervisor Sênior",
    "hierarchyLevel": 18
  }'
```

### Deletar Role

```bash
curl -X DELETE http://localhost:5000/api/v1/roles/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer {token}"
```

### Listar Funcionários

```bash
curl -X GET http://localhost:5000/api/employees \
  -H "Authorization: Bearer {token}"
```

### Criar Funcionário

```bash
curl -X POST http://localhost:5000/api/employees \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "João",
    "lastName": "Silva",
    "email": "joao@empresa.com",
    "documentNumber": "12345678900",
    "birthDate": "1990-01-15",
    "password": "Senha@123456",
    "role": 1,
    "phoneNumbers": ["11999999999"]
  }'
```

## Swagger UI

Documentação interativa disponível em:
- **Development**: http://localhost:5000/swagger
- **Docker**: http://localhost:5000/swagger

## Postman Collection

Collection disponível em: `docs/postman/collections/EmployeeManagement.postman_collection.json`

### Importar no Postman

1. Abrir Postman
2. File → Import
3. Selecionar o arquivo `.json`
4. Configurar environment (Development ou Production)

## Próximos Passos

- [Autenticação](07-AUTENTICACAO.md)
- [Guia de Desenvolvimento](13-GUIA-DESENVOLVIMENTO.md)
- [Troubleshooting](15-TROUBLESHOOTING.md)

