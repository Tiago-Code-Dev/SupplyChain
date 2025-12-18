# language: pt-BR
Funcionalidade: Validações de Campos
  Como um sistema robusto
  Eu quero validar todos os campos de entrada
  Para garantir a integridade dos dados

  @validacao @email
  Cenário: Validar formato de email inválido sem arroba
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com email "email-sem-arroba"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de email inválido

  @validacao @email
  Cenário: Validar formato de email inválido sem domínio
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com email "usuario@"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de email inválido

  @validacao @email
  Cenário: Validar formato de email inválido com espaços
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com email "usuario @email.com"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de email inválido

  @validacao @telefone
  Cenário: Validar formato de telefone muito curto
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com telefone "123"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de telefone inválido

  @validacao @telefone
  Cenário: Validar formato de telefone com letras
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com telefone "11ABCD99999"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de telefone inválido

  @validacao @documento
  Cenário: Validar documento CPF com formato inválido
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com documento "123"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de documento inválido

  @validacao @documento
  Cenário: Validar documento CPF com letras
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com documento "123ABC45678"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de documento inválido

  @validacao @documento
  Cenário: Validar documento CPF com todos dígitos iguais
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com documento "11111111111"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando documento inválido

  @validacao @senha
  Cenário: Validar senha muito curta
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com senha "123"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que a senha é muito curta

  @validacao @senha
  Cenário: Validar senha sem caracteres especiais
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com senha "Senha123456"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que a senha deve conter caracteres especiais

  @validacao @senha
  Cenário: Validar senha sem números
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com senha "Senha@Forte"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que a senha deve conter números

  @validacao @senha
  Cenário: Validar senha sem letras maiúsculas
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com senha "senha@123456"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que a senha deve conter letras maiúsculas

  @validacao @data
  Cenário: Validar data de nascimento no futuro
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com data de nascimento "2030-01-01"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando data de nascimento inválida

  @validacao @data
  Cenário: Validar data de nascimento muito antiga
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com data de nascimento "1800-01-01"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando data de nascimento inválida

  @validacao @nome
  Cenário: Validar nome com números
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com nome "João123"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de nome inválido

  @validacao @nome
  Cenário: Validar nome muito curto
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com nome "A"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que o nome deve ter pelo menos 2 caracteres

  @validacao @nome
  Cenário: Validar sobrenome muito longo
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um funcionário com sobrenome de 300 caracteres
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando que o sobrenome excede o limite de caracteres