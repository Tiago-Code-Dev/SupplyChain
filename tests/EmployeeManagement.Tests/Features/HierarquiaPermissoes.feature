# language: pt-BR
Funcionalidade: Hierarquia de Permissões
  Como um administrador do sistema
  Eu quero garantir que a hierarquia de permissões seja respeitada
  Para manter a segurança e governança do sistema

  # Hierarquia: Admin > Director > Manager > Leader > Employee

  @hierarquia @criar @sucesso
  Cenário: Admin pode criar qualquer nível de permissão
    Dado que o usuário está autenticado como "Admin"
    Quando o usuário cria um novo funcionário com permissão "Director"
    Então o funcionário deve ser criado com sucesso
    E o funcionário deve ter permissão "Director"

  @hierarquia @criar @sucesso
  Cenário: Director pode criar Manager
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com permissão "Manager"
    Então o funcionário deve ser criado com sucesso
    E o funcionário deve ter permissão "Manager"

  @hierarquia @criar @sucesso
  Cenário: Director pode criar Employee
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com permissão "Employee"
    Então o funcionário deve ser criado com sucesso
    E o funcionário deve ter permissão "Employee"

  @hierarquia @criar @sucesso
  Cenário: Manager pode criar Employee
    Dado que o usuário está autenticado como "Manager"
    Quando o usuário cria um novo funcionário com permissão "Employee"
    Então o funcionário deve ser criado com sucesso
    E o funcionário deve ter permissão "Employee"

  @hierarquia @criar @falha
  Cenário: Director não pode criar Admin
    Dado que o usuário está autenticado como "Director"
    Quando o usuário tenta criar um novo funcionário com permissão "Admin"
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para criar usuários com nível de permissão superior"
    E o funcionário não deve ser criado no banco de dados

  @hierarquia @criar @falha
  Cenário: Manager não pode criar Director
    Dado que o usuário está autenticado como "Manager"
    Quando o usuário tenta criar um novo funcionário com permissão "Director"
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para criar usuários com nível de permissão superior"
    E o funcionário não deve ser criado no banco de dados

  @hierarquia @criar @falha
  Cenário: Manager não pode criar Manager
    Dado que o usuário está autenticado como "Manager"
    Quando o usuário tenta criar um novo funcionário com permissão "Manager"
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para criar usuários com nível de permissão igual ou superior"
    E o funcionário não deve ser criado no banco de dados

  @hierarquia @criar @falha
  Cenário: Employee não pode criar nenhum funcionário
    Dado que o usuário está autenticado como "Employee"
    Quando o usuário tenta criar um novo funcionário com permissão "Employee"
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para criar funcionários"
    E o funcionário não deve ser criado no banco de dados

  @hierarquia @atualizar @falha
  Cenário: Manager não pode promover Employee para Director
    Dado que o usuário está autenticado como "Manager"
    E que existe um funcionário com permissão "Employee"
    Quando o usuário tenta atualizar o funcionário para permissão "Director"
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para alterar usuários para nível de permissão superior"
    E o funcionário não deve ser atualizado no banco de dados

  @hierarquia @atualizar @sucesso
  Cenário: Admin pode alterar qualquer permissão
    Dado que o usuário está autenticado como "Admin"
    E que existe um funcionário com permissão "Employee"
    Quando o usuário atualiza o funcionário para permissão "Director"
    Então o sistema deve retornar status 200
    E o funcionário deve ter permissão "Director"

  @hierarquia @excluir @falha
  Cenário: Employee não pode excluir outros funcionários
    Dado que o usuário está autenticado como "Employee"
    E que existe um funcionário cadastrado para exclusão
    Quando o usuário tenta excluir o funcionário
    Então o sistema deve retornar status 403
    E o sistema deve retornar mensagem "Você não tem permissão para excluir funcionários"

  @hierarquia @excluir @sucesso
  Cenário: Director pode excluir Employee
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário com permissão "Employee" para exclusão
    Quando o usuário exclui o funcionário
    Então o sistema deve retornar status 204
    E o funcionário deve ser marcado como excluído

  @hierarquia @excluir @sucesso
  Cenário: Admin pode excluir qualquer funcionário
    Dado que o usuário está autenticado como "Admin"
    E que existe um funcionário com permissão "Director" para exclusão
    Quando o usuário exclui o funcionário
    Então o sistema deve retornar status 204
    E o funcionário deve ser marcado como excluído