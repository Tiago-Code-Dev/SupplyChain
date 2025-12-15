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
      | Nome  | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João  | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 201
    E o sistema deve retornar os dados do funcionário criado
    E o funcionário deve ter um ID único gerado
    E a senha do funcionário deve estar hasheada no banco de dados

  @funcionario @criar @sucesso
  Cenário: Criar funcionário com apenas um telefone
    Dado que o usuário está autenticado como "Director"
    E que não existe funcionário com documento "12345678900"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 201
    E o funcionário deve ser criado com sucesso
    E o funcionário deve ter exatamente 1 telefone cadastrado

  @funcionario @criar @sucesso
  Cenário: Criar funcionário com múltiplos telefones
    Dado que o usuário está autenticado como "Director"
    E que não existe funcionário com documento "12345678900"
    Quando o usuário cria um novo funcionário com telefones:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Permissao | Senha        |
      | João | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | Employee  | Senha@123456 |
    E os telefones são "11999999999,11888888888,11777777777"
    Então o sistema deve retornar status 201
    E o funcionário deve ser criado com sucesso
    E o funcionário deve ter 3 telefones cadastrados

  @funcionario @criar @autenticacao
  Cenário: Criar funcionário sem autenticação
    Dado que o usuário não está autenticado
    Quando o usuário tenta criar um novo funcionário com dados válidos
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Não autorizado"
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com nome vazio
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      |      | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem de erro indicando que nome é obrigatório
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com sobrenome vazio
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João |           | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem de erro indicando que sobrenome é obrigatório
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com email inválido
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email          | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João | Silva     | email-invalido | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem de erro indicando que email é inválido
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com email vazio
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João | Silva     |       | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem de erro indicando que email é obrigatório
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @conflito
  Cenário: Criar funcionário com documento duplicado
    Dado que o usuário está autenticado como "Director"
    E que já existe um funcionário cadastrado com documento "12345678900"
    Quando o usuário tenta criar um novo funcionário com:
      | Nome  | Sobrenome | Email            | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | Maria | Santos    | maria@supply.com | 12345678900 | 1992-05-20     | 11777777777 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 409
    E o sistema deve retornar mensagem "Documento já cadastrado"
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com documento vazio
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email           | Documento | DataNascimento | Telefones   | Permissao | Senha        |
      | João | Silva     | joao@supply.com |           | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem de erro indicando que documento é obrigatório
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao @negocio
  Cenário: Criar funcionário menor de idade
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome  | Sobrenome | Email            | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | Pedro | Costa     | pedro@supply.com | 98765432100 | 2010-06-15     | 11666666666 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Funcionário deve ser maior de idade"
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário sem telefone
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário sem telefones:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Permissao | Senha        |
      | João | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Funcionário deve possuir pelo menos um telefone"
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @conflito
  Cenário: Criar funcionário com email duplicado
    Dado que o usuário está autenticado como "Director"
    E que já existe um funcionário cadastrado com email "joao@supply.com"
    Quando o usuário tenta criar um novo funcionário com:
      | Nome  | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | Outro | João      | joao@supply.com | 98765432100 | 1992-05-20     | 11777777777 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 409
    E o sistema deve retornar mensagem "Email já cadastrado"
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com gestor inexistente
    Dado que o usuário está autenticado como "Director"
    E que não existe gestor com ID "99999999-9999-9999-9999-999999999999"
    Quando o usuário tenta criar um novo funcionário com gestor inexistente:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha        |
      | João | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | Senha@123456 |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Gestor não encontrado"
    E o funcionário não deve ser criado no banco de dados

  @funcionario @criar @validacao
  Cenário: Criar funcionário com senha fraca
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com:
      | Nome | Sobrenome | Email           | Documento   | DataNascimento | Telefones   | Permissao | Senha |
      | João | Silva     | joao@supply.com | 12345678900 | 1990-01-15     | 11999999999 | Employee  | 123   |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que a senha não atende aos critérios de segurança
    E o funcionário não deve ser criado no banco de dados
