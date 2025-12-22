# Employee Management Frontend

Frontend React + TypeScript para o sistema de gerenciamento de funcionários.

## 🚀 Tecnologias

- **React 19** - Biblioteca UI
- **TypeScript** - Tipagem estática
- **Vite** - Build tool
- **React Router** - Roteamento
- **Zustand** - Gerenciamento de estado
- **Axios** - Cliente HTTP
- **React Hook Form** - Formulários
- **Zod** - Validação
- **Tailwind CSS** - Estilização
- **Heroicons** - Ícones

## 📁 Estrutura do Projeto

```
frontend/
├── src/
│   ├── app/              # Configuração da aplicação
│   │   └── App.tsx       # Componente principal e rotas
│   ├── features/         # Features da aplicação
│   │   ├── auth/         # Autenticação
│   │   │   ├── services/ # Serviços de API
│   │   │   └── store/    # Estado global
│   │   └── employees/     # Funcionários
│   │       └── services/ # Serviços de API
│   ├── pages/            # Páginas da aplicação
│   │   ├── LoginPage.tsx
│   │   ├── EmployeesPage.tsx
│   │   ├── EmployeeFormPage.tsx
│   │   └── EmployeeDetailPage.tsx
│   └── shared/           # Código compartilhado
│       ├── components/   # Componentes reutilizáveis
│       ├── config/       # Configurações
│       ├── services/     # Serviços compartilhados
│       ├── types/        # Tipos TypeScript
│       └── utils/         # Utilitários
```

## 🛠️ Instalação

```bash
npm install
```

## 🚀 Execução

```bash
# Desenvolvimento
npm run dev

# Build para produção
npm run build

# Preview do build
npm run preview
```

## 🔧 Configuração

Crie um arquivo `.env` na raiz do projeto:

```env
VITE_API_BASE_URL=http://localhost:5000
```

## 📝 Funcionalidades

- ✅ Autenticação JWT com refresh token
- ✅ Listagem paginada de funcionários
- ✅ Busca e filtros
- ✅ Criação de funcionários
- ✅ Edição de funcionários
- ✅ Exclusão de funcionários
- ✅ Detalhes do funcionário
- ✅ Controle de permissões baseado em roles
- ✅ Tratamento de erros
- ✅ Loading states
- ✅ UI responsiva

## 🔐 Credenciais Padrão

- Email: `admin@empresa.com`
- Senha: `Admin@123`

## 📚 API

O frontend consome a API REST disponível em `http://localhost:5000`.

### Endpoints principais:

- `POST /api/auth/login` - Login
- `GET /api/auth/me` - Informações do usuário
- `GET /api/employees` - Lista de funcionários
- `POST /api/employees` - Criar funcionário
- `PUT /api/employees/:id` - Atualizar funcionário
- `DELETE /api/employees/:id` - Excluir funcionário
