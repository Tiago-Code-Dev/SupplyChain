# language: pt-BR
Funcionalidade: Atualização de Funcionário
  Como um usuário autorizado do sistema
  Eu quero atualizar os dados de funcionários
  Para manter as informações atualizadas

  @funcionario @atualizar @sucesso
  Cenário: Atualizar funcionário com dados válidos
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido e nome "João Silva"
    Quando o usuário atualiza o funcionário com:
      | Nome | Sobrenome | Email                    | Telefones               |
      | João | Oliveira  | joao.oliveira@supply.com | 11999999999,11888888888 |
    Então o sistema deve retornar status 200
    E o sistema deve retornar os dados atualizados do funcionário
    E o funcionário no banco de dados deve ter os novos dados salvos

  @funcionario @atualizar @falha
  Cenário: Atualizar funcionário inexistente
    Dado que o usuário está autenticado como "Director"
    E que não existe funcionário com ID "99999999-9999-9999-9999-999999999999"
    Quando o usuário tenta atualizar o funcionário inexistente com dados válidos
    Então o sistema deve retornar status 404
    E o sistema deve retornar mensagem "Employee 'Funcionário não encontrado' was not found"

  @funcionario @atualizar @autenticacao
  Cenário: Atualizar funcionário sem autenticação
    Dado que o usuário não está autenticado
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário tenta atualizar o funcionário com dados válidos sem autenticação
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Não autorizado"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @conflito
  Cenário: Atualizar funcionário com documento duplicado de outro funcionário
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário com ID conhecido e documento "11111111111"
    E que existe outro funcionário com documento "22222222222"
    Quando o usuário tenta atualizar o funcionário para documento "22222222222"
    Então o sistema deve retornar status 409
    E o sistema deve retornar mensagem "Documento já cadastrado para outro funcionário"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @sucesso
  Cenário: Atualizar funcionário mantendo seu próprio documento
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido e documento "11111111111"
    Quando o usuário atualiza o funcionário mantendo o documento "11111111111" e alterando outros campos
    Então o sistema deve retornar status 200
    E o funcionário deve ser atualizado com sucesso
    E o documento deve permanecer "11111111111"

  @funcionario @atualizar @conflito
  Cenário: Atualizar funcionário com email duplicado de outro funcionário
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário com ID conhecido e email "joao@supply.com"
    E que existe outro funcionário com email "maria@supply.com"
    Quando o usuário tenta atualizar o funcionário para email "maria@supply.com"
    Então o sistema deve retornar status 409
    E o sistema deve retornar mensagem "Email já cadastrado"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @validacao @negocio
  Cenário: Atualizar funcionário para menor de idade
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido e data de nascimento "1990-01-15"
    Quando o usuário tenta atualizar o funcionário com data de nascimento "2010-06-15"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Employee must be at least 18 years old"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @validacao
  Cenário: Atualizar funcionário removendo todos os telefones
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido e telefones "11999999999,11888888888"
    Quando o usuário tenta atualizar o funcionário removendo todos os telefones
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Funcionário deve possuir pelo menos um telefone"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @validacao
  Cenário: Atualizar funcionário com gestor inexistente
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    E que não existe gestor com ID "99999999-9999-9999-9999-999999999999"
    Quando o usuário tenta atualizar o funcionário com gestor inexistente
    Então o sistema deve retornar status 404
    E o sistema deve retornar mensagem "Gestor não encontrado"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @validacao @negocio
  Cenário: Atualizar funcionário para ser seu próprio gestor
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário tenta atualizar o funcionário para ser seu próprio gestor
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "O funcionário não pode ser seu próprio gestor"
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @validacao
  Cenário: Atualizar funcionário com email inválido
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário tenta atualizar o funcionário com email "email-invalido"
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem indicando formato de email inválido
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @validacao
  Cenário: Atualizar funcionário com nome vazio
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário tenta atualizar o funcionário com nome vazio
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem de erro indicando que nome é obrigatório
    E o funcionário não deve ser atualizado no banco de dados

  @funcionario @atualizar @cache
  Cenário: Atualização deve invalidar cache do funcionário
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    E que os dados do funcionário estão em cache
    Quando o usuário atualiza o funcionário com dados válidos
    Então o sistema deve retornar status 200
    E o cache do funcionário deve ser invalidado