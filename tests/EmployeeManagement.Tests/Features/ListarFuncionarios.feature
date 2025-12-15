# language: pt-BR
Funcionalidade: Listagem de Funcionários
  Como um usuário autenticado do sistema
  Eu quero listar e buscar funcionários
  Para visualizar as informações cadastradas

  @funcionario @listar @sucesso
  Cenário: Listar todos os funcionários
    Dado que o usuário está autenticado como "Employee"
    E que existem 5 funcionários cadastrados no sistema
    Quando o usuário solicita a listagem de funcionários através do endpoint GET /api/employees
    Então o sistema deve retornar status 200
    E o sistema deve retornar uma lista com 5 funcionários
    E cada funcionário na lista deve conter: ID, Nome, Sobrenome, Email, Documento, Telefones, Permissao
    E a senha não deve estar presente na resposta

  @funcionario @listar @paginacao
  Cenário: Listar funcionários com paginação
    Dado que o usuário está autenticado como "Employee"
    E que existem 25 funcionários cadastrados no sistema
    Quando o usuário solicita a listagem de funcionários com:
      | Page | PageSize |
      | 1    | 10       |
    Então o sistema deve retornar status 200
    E o sistema deve retornar 10 funcionários
    E a resposta deve conter informações de paginação

  @funcionario @listar @paginacao
  Cenário: Listar segunda página de funcionários
    Dado que o usuário está autenticado como "Employee"
    E que existem 25 funcionários cadastrados no sistema
    Quando o usuário solicita a listagem de funcionários com:
      | Page | PageSize |
      | 2    | 10       |
    Então o sistema deve retornar status 200
    E o sistema deve retornar 10 funcionários
    E os funcionários devem ser diferentes da primeira página

  @funcionario @listar @filtro
  Cenário: Listar funcionários com filtro por nome
    Dado que o usuário está autenticado como "Employee"
    E que existem funcionários cadastrados:
      | Nome  | Sobrenome |
      | João  | Silva     |
      | Maria | Santos    |
      | João  | Costa     |
    Quando o usuário solicita a listagem de funcionários com filtro por nome "João"
    Então o sistema deve retornar status 200
    E o sistema deve retornar apenas funcionários cujo nome contenha "João"
    E a lista deve conter pelo menos 2 funcionários

  @funcionario @listar @filtro
  Cenário: Listar funcionários com filtro por email
    Dado que o usuário está autenticado como "Employee"
    E que existem funcionários cadastrados:
      | Email            |
      | joao@supply.com  |
      | maria@supply.com |
      | pedro@supply.com |
    Quando o usuário solicita a listagem de funcionários com filtro por email "joao@supply.com"
    Então o sistema deve retornar status 200
    E o sistema deve retornar apenas funcionários com email "joao@supply.com"
    E a lista deve conter exatamente 1 funcionário

  @funcionario @listar @filtro
  Cenário: Listar funcionários com filtro por permissão
    Dado que o usuário está autenticado como "Employee"
    E que existem funcionários cadastrados:
      | Nome  | Permissao |
      | João  | Employee  |
      | Maria | Manager   |
      | Pedro | Admin     |
    Quando o usuário solicita a listagem de funcionários com filtro por permissão "Manager"
    Então o sistema deve retornar status 200
    E o sistema deve retornar apenas funcionários com permissão "Manager"

  @funcionario @buscar @sucesso
  Cenário: Buscar funcionário por ID existente
    Dado que o usuário está autenticado como "Employee"
    E que existe um funcionário cadastrado com ID conhecido e nome "João Silva"
    Quando o usuário solicita os dados do funcionário através do endpoint GET /api/employees/{id}
    Então o sistema deve retornar status 200
    E o sistema deve retornar os dados completos do funcionário
    E a resposta deve conter: ID, Nome, Sobrenome, Email, Documento, Telefones, Permissao
    E a senha não deve estar presente na resposta

  @funcionario @buscar @falha
  Cenário: Buscar funcionário por ID inexistente
    Dado que o usuário está autenticado como "Employee"
    E que não existe funcionário com ID "99999999-9999-9999-9999-999999999999"
    Quando o usuário solicita os dados do funcionário através do endpoint GET /api/employees/{id}
    Então o sistema deve retornar status 404
    E o sistema deve retornar mensagem "Funcionário não encontrado"

  @funcionario @listar @autenticacao
  Cenário: Listar funcionários sem autenticação
    Dado que o usuário não está autenticado
    Quando o usuário tenta listar funcionários através do endpoint GET /api/employees
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Não autorizado"

  @funcionario @buscar @cache
  Cenário: Buscar funcionário deve usar cache quando disponível
    Dado que o usuário está autenticado como "Employee"
    E que existe um funcionário cadastrado com ID conhecido
    E que os dados do funcionário estão em cache
    Quando o usuário solicita os dados do funcionário através do endpoint GET /api/employees/{id}
    Então o sistema deve retornar status 200
    E os dados devem ser recuperados do cache
    E o banco de dados não deve ser consultado

  @funcionario @listar @ordenacao
  Cenário: Listar funcionários ordenados por nome
    Dado que o usuário está autenticado como "Employee"
    E que existem funcionários cadastrados:
      | Nome   |
      | Carlos |
      | Ana    |
      | Bruno  |
    Quando o usuário solicita a listagem de funcionários ordenada por nome ascendente
    Então o sistema deve retornar status 200
    E os funcionários devem estar ordenados alfabeticamente por nome
