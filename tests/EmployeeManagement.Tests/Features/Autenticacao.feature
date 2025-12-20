# language: pt-BR
Funcionalidade: Autenticação e Autorização
  Como um usuário do sistema
  Eu quero me autenticar com minhas credenciais
  Para acessar as funcionalidades protegidas

  @autenticacao @sucesso
  Cenário: Login bem-sucedido com credenciais válidas
    Dado que existe um funcionário cadastrado no sistema com:
      | Email           | Senha        |
      | joao@supply.com | Senha@123456 |
    Quando o usuário realiza login com:
      | Email           | Senha        |
      | joao@supply.com | Senha@123456 |
    Então o sistema deve retornar status 200
    E o sistema deve retornar um token JWT válido
    E o token deve conter as claims de identificação do usuário
    E o token deve conter a claim de permissão do usuário

  @autenticacao @falha
  Cenário: Login falha com email inexistente
    Dado que não existe um funcionário cadastrado com email "inexistente@supply.com"
    Quando o usuário realiza login com:
      | Email                  | Senha        |
      | inexistente@supply.com | Senha@123456 |
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Credenciais inválidas"
    E o sistema não deve retornar token JWT

  @autenticacao @falha
  Cenário: Login falha com senha incorreta
    Dado que existe um funcionário cadastrado no sistema com:
      | Email           | Senha        |
      | joao@supply.com | Senha@123456 |
    Quando o usuário realiza login com:
      | Email           | Senha       |
      | joao@supply.com | SenhaErrada |
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Credenciais inválidas"
    E o sistema não deve retornar token JWT

  @autenticacao @seguranca
  Cenário: Acesso negado sem token de autenticação
    Dado que o usuário não possui token de autenticação
    Quando o usuário tenta acessar o endpoint GET /api/employees
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Não autorizado"

  @autenticacao @seguranca
  Cenário: Acesso negado com token expirado
    Dado que o usuário possui um token JWT expirado
    Quando o usuário tenta acessar o endpoint GET /api/employees
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Token expirado"

  @autenticacao @seguranca
  Cenário: Mensagem de erro genérica não revela existência de email
    Dado que existe um funcionário cadastrado no sistema com:
      | Email           | Senha        |
      | joao@supply.com | Senha@123456 |
    Quando o usuário realiza login com email "joao@supply.com" e senha "SenhaErrada"
    Então o sistema deve retornar mensagem "Credenciais inválidas"
    Quando o usuário realiza login com email "naoexiste@supply.com" e senha "Qualquer123"
    Então o sistema deve retornar mensagem "Credenciais inválidas"
