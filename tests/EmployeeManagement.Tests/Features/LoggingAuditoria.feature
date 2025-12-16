# language: pt-BR
Funcionalidade: Logging e Auditoria
  Como um administrador do sistema
  Eu quero que todas as operações sejam registradas em log
  Para garantir rastreabilidade e auditoria

  @logging @criar
  Cenário: Registrar log ao criar funcionário
    Dado que o usuário está autenticado como "Director"
    E que o sistema de logging está configurado
    Quando o usuário cria um novo funcionário com sucesso
    Então o sistema deve registrar um log com nível "Information"
    E o log deve conter a operação realizada "CreateEmployee"
    E o log deve conter o ID do usuário que executou a operação
    E o log deve conter o ID do funcionário criado
    E o log deve conter timestamp da operação

  @logging @atualizar
  Cenário: Registrar log ao atualizar funcionário
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    E que o sistema de logging está configurado
    Quando o usuário atualiza o funcionário com sucesso
    Então o sistema deve registrar um log com nível "Information"
    E o log deve conter a operação realizada "UpdateEmployee"
    E o log deve conter o ID do funcionário atualizado
    E o log deve conter os campos alterados
    E o log deve conter timestamp da operação

  @logging @excluir
  Cenário: Registrar log ao excluir funcionário
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado para exclusão
    E que o sistema de logging está configurado
    Quando o usuário exclui o funcionário com sucesso
    Então o sistema deve registrar um log com nível "Warning"
    E o log deve conter a operação realizada "DeleteEmployee"
    E o log deve conter o ID do funcionário excluído
    E o log deve conter timestamp da operação

  @logging @seguranca
  Cenário: Registrar log de tentativa de acesso não autorizado
    Dado que o usuário não está autenticado
    E que o sistema de logging está configurado
    Quando o usuário tenta acessar um endpoint protegido
    Então o sistema deve registrar um log com nível "Warning"
    E o log deve conter "Unauthorized access attempt"
    E o log deve conter o endpoint acessado
    E o log deve conter timestamp da operação

  @logging @login
  Cenário: Registrar log de login bem-sucedido
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "Senha@123"
    E que o sistema de logging está configurado
    Quando o usuário realiza login com sucesso
    Então o sistema deve registrar um log com nível "Information"
    E o log deve conter "Login successful"
    E o log deve conter o email do usuário
    E o log deve conter timestamp da operação

  @logging @login @falha
  Cenário: Registrar log de tentativa de login falha
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "Senha@123"
    E que o sistema de logging está configurado
    Quando o usuário tenta fazer login com senha incorreta
    Então o sistema deve registrar um log com nível "Warning"
    E o log deve conter "Login failed"
    E o log deve conter o email tentado
    E o log deve conter timestamp da operação

  @logging @senha
  Cenário: Registrar log ao alterar senha
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "SenhaAtual@123"
    E que o sistema de logging está configurado
    Quando o funcionário altera sua senha com sucesso
    Então o sistema deve registrar um log com nível "Information"
    E o log deve conter "Credentials updated"
    E o log deve conter o ID do funcionário
    E o log deve conter timestamp da operação
    E o log NÃO deve conter a senha antiga ou nova

  @logging @erro
  Cenário: Registrar log de erro interno
    Dado que o usuário está autenticado como "Director"
    E que o sistema de logging está configurado
    Quando ocorre um erro interno durante uma operação
    Então o sistema deve registrar um log com nível "Error"
    E o log deve conter a stack trace do erro
    E o log deve conter o contexto da operação
    E o log deve conter timestamp da operação

  @auditoria @historico
  Cenário: Manter histórico de alterações do funcionário
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário atualiza o nome do funcionário de "João" para "Carlos"
    Então o sistema deve registrar a alteração no histórico de auditoria
    E o histórico deve conter o valor anterior "João"
    E o histórico deve conter o novo valor "Carlos"
    E o histórico deve conter o usuário que fez a alteração
    E o histórico deve conter a data da alteração