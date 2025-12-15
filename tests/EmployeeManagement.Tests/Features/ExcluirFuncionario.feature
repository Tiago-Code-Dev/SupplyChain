# language: pt-BR
Funcionalidade: Exclusão de Funcionário
  Como um usuário autorizado do sistema
  Eu quero excluir funcionários
  Para remover acessos de pessoas desligadas

  @funcionario @excluir @sucesso
  Cenário: Excluir funcionário com sucesso
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido para exclusão
    Quando o usuário exclui o funcionário através do endpoint DELETE /api/employees/{id}
    Então o sistema deve retornar status 204
    E o funcionário deve ser marcado como excluído (soft delete)
    E a data de exclusão deve ser registrada

  @funcionario @excluir @falha
  Cenário: Excluir funcionário inexistente
    Dado que o usuário está autenticado como "Director"
    E que não existe funcionário com ID "99999999-9999-9999-9999-999999999999"
    Quando o usuário tenta excluir o funcionário inexistente
    Então o sistema deve retornar status 404
    E o sistema deve retornar mensagem "Funcionário não encontrado"

  @funcionario @excluir @autenticacao
  Cenário: Excluir funcionário sem autenticação
    Dado que o usuário não está autenticado
    E que existe um funcionário cadastrado com ID conhecido para exclusão
    Quando o usuário tenta excluir o funcionário sem autenticação
    Então o sistema deve retornar status 401
    E o sistema deve retornar mensagem "Não autorizado"
    E o funcionário não deve ser excluído do banco de dados

  @funcionario @excluir @autorizacao @hierarquia
  Cenário: Funcionário sem permissão tenta excluir outro funcionário
    Dado que o usuário está autenticado como "Employee"
    E que existe um funcionário cadastrado com ID conhecido para exclusão
    Quando o usuário tenta excluir o funcionário
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para excluir funcionários"
    E o funcionário não deve ser excluído do banco de dados

  @funcionario @excluir @negocio
  Cenário: Excluir funcionário que é gestor de outros
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido que é gestor
    E que existem funcionários subordinados a este gestor
    Quando o usuário tenta excluir o funcionário gestor
    Então o sistema deve retornar status 400
    E o sistema deve retornar mensagem "Não é possível excluir funcionário que possui subordinados"
    E o funcionário não deve ser excluído do banco de dados

  @funcionario @excluir @cache
  Cenário: Exclusão deve invalidar cache
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido para exclusão
    E que os dados do funcionário estão em cache
    Quando o usuário exclui o funcionário
    Então o sistema deve retornar status 204
    E o cache do funcionário deve ser invalidado
    E o cache da lista de funcionários deve ser invalidado

  @funcionario @excluir @softdelete
  Cenário: Funcionário excluído não aparece em listagens
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido para exclusão
    Quando o usuário exclui o funcionário
    E o usuário solicita a listagem de funcionários
    Então o funcionário excluído não deve aparecer na listagem

  @funcionario @excluir @softdelete
  Cenário: Soft delete preserva dados no banco
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido para exclusão
    Quando o usuário exclui o funcionário
    Então o sistema deve retornar status 204
    E o registro do funcionário deve existir no banco de dados
    E o campo IsDeleted deve ser true
    E o campo DeletedAt deve estar preenchido