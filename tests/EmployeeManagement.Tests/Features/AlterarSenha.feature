# language: pt-BR
Funcionalidade: Alteração de Senha
  Como um funcionário do sistema
  Eu quero alterar minha senha
  Para manter minha conta segura

  @senha @alterar @sucesso
  Cenário: Alterar senha com sucesso
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário está autenticado
    Quando o funcionário solicita alteração de senha informando:
      | SenhaAtual     | NovaSenha      |
      | SenhaAtual@123 | NovaSenha@456  |
    Então a senha deve ser alterada com sucesso
    E o sistema deve retornar status 200
    E a nova senha deve estar hasheada no banco de dados

  @senha @alterar @falha
  Cenário: Não deve alterar senha quando senha atual está incorreta
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário está autenticado
    Quando o funcionário solicita alteração de senha informando:
      | SenhaAtual     | NovaSenha      |
      | SenhaErrada123 | NovaSenha@456  |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Current password is incorrect"
    E a senha não deve ser alterada

  @senha @alterar @validacao
  Cenário: Não deve alterar para senha vazia
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário está autenticado
    Quando o funcionário solicita alteração de senha informando:
      | SenhaAtual     | NovaSenha |
      | SenhaAtual@123 |           |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Nova senha é obrigatória"
    E a senha não deve ser alterada

  @senha @alterar @validacao
  Cenário: Não deve alterar para senha fraca
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário está autenticado
    Quando o funcionário solicita alteração de senha informando:
      | SenhaAtual     | NovaSenha |
      | SenhaAtual@123 | 123       |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que a senha não atende aos critérios de segurança
    E a senha não deve ser alterada

  @senha @alterar @validacao
  Cenário: Não deve alterar para mesma senha atual
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário está autenticado
    Quando o funcionário solicita alteração de senha informando:
      | SenhaAtual     | NovaSenha       |
      | SenhaAtual@123 | SenhaAtual@123  |
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Nova senha deve ser diferente da atual"
    E a senha não deve ser alterada

  @senha @alterar @autenticacao
  Cenário: Não deve alterar senha de funcionário inexistente
    Dado que não existe funcionário com ID "99999999-9999-9999-9999-999999999999"
    Quando é solicitada alteração de senha do funcionário inexistente
    Então o sistema deve retornar status 404
    E o sistema deve retornar mensagem "Employee with ID '99999999-9999-9999-9999-999999999999"

  @senha @alterar @autenticacao
  Cenário: Não deve alterar senha sem autenticação
    Dado que existe um funcionario cadastrado com email "usuario@supply.com"
    E que o usuário não está autenticado
    Quando o usuário tenta alterar a senha sem autenticação
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Não autorizado"

  @senha @alterar @seguranca
  Cenário: Alteração de senha deve gerar evento de domínio
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário está autenticado
    Quando o funcionário solicita alteração de senha informando:
      | SenhaAtual     | NovaSenha      |
      | SenhaAtual@123 | NovaSenha@456  |
    Então a senha deve ser alterada com sucesso
    E o evento PasswordChangedEvent deve ser disparado

  @senha @alterar @seguranca
  Cenário: Deve invalidar sessões anteriores após alteração de senha
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o funcionário possui sessões ativas
    Quando o funcionário altera sua senha com sucesso
    Então todas as sessões anteriores devem ser invalidadas
    E apenas a sessão atual deve permanecer válida