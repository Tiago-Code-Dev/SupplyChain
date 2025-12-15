# language: pt-BR
Funcionalidade: Performance e Escalabilidade
  Como um sistema de produção
  Eu quero garantir tempos de resposta adequados
  Para proporcionar boa experiência ao usuário

  @performance @listagem
  Cenário: Listar funcionários com grande volume de dados
    Dado que o usuário está autenticado como "Employee"
    E que existem 1000 funcionários cadastrados no sistema
    Quando o usuário solicita a listagem de funcionários com paginação de 10 por página
    Então o sistema deve retornar status 200
    E o tempo de resposta deve ser menor que 500ms
    E o sistema deve retornar apenas 10 funcionários
    E a resposta deve incluir informações de paginação

  @performance @busca
  Cenário: Buscar funcionário por ID com resposta rápida
    Dado que o usuário está autenticado como "Employee"
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário solicita os dados do funcionário por ID
    Então o sistema deve retornar status 200
    E o tempo de resposta deve ser menor que 200ms

  @performance @cache
  Cenário: Busca por ID deve usar cache para melhorar performance
    Dado que o usuário está autenticado como "Employee"
    E que existe um funcionário cadastrado com ID conhecido
    E que os dados estão em cache
    Quando o usuário solicita os dados do funcionário por ID
    Então o tempo de resposta deve ser menor que 50ms
    E os dados devem vir do cache
    E o banco de dados não deve ser consultado

  @performance @filtro
  Cenário: Filtrar funcionários por nome com performance adequada
    Dado que o usuário está autenticado como "Employee"
    E que existem 1000 funcionários cadastrados no sistema
    Quando o usuário filtra funcionários pelo nome "João"
    Então o sistema deve retornar status 200
    E o tempo de resposta deve ser menor que 300ms

  @performance @criacao
  Cenário: Criar funcionário com tempo de resposta adequado
    Dado que o usuário está autenticado como "Director"
    Quando o usuário cria um novo funcionário com dados válidos
    Então o sistema deve retornar status 201
    E o tempo de resposta deve ser menor que 500ms
    E o hash da senha deve ser gerado de forma segura

  @performance @atualizacao
  Cenário: Atualizar funcionário com tempo de resposta adequado
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado com ID conhecido
    Quando o usuário atualiza o funcionário com dados válidos
    Então o sistema deve retornar status 200
    E o tempo de resposta deve ser menor que 300ms

  @performance @exclusao
  Cenário: Excluir funcionário com tempo de resposta adequado
    Dado que o usuário está autenticado como "Director"
    E que existe um funcionário cadastrado para exclusão
    Quando o usuário exclui o funcionário
    Então o sistema deve retornar status 204
    E o tempo de resposta deve ser menor que 200ms

  @performance @login
  Cenário: Login deve ter tempo de resposta adequado
    Dado que existe um funcionário cadastrado com email "usuario@supply.com" e senha "Senha@123"
    Quando o usuário realiza login com credenciais válidas
    Então o sistema deve retornar status 200
    E o tempo de resposta deve ser menor que 500ms
    E a verificação de senha deve usar algoritmo seguro

  @escalabilidade @concorrencia
  Cenário: Sistema deve suportar múltiplas requisições simultâneas
    Dado que o usuário está autenticado como "Employee"
    E que existem 100 funcionários cadastrados no sistema
    Quando 50 usuários fazem requisições simultâneas de listagem
    Então todas as requisições devem retornar status 200
    E nenhuma requisição deve exceder 2 segundos

  @escalabilidade @memoria
  Cenário: Listagem paginada deve ter uso eficiente de memória
    Dado que o usuário está autenticado como "Employee"
    E que existem 10000 funcionários cadastrados no sistema
    Quando o usuário solicita a listagem com paginação de 20 por página
    Então o sistema deve carregar apenas os 20 registros solicitados
    E o uso de memória deve permanecer estável

  @performance @indice
  Cenário: Busca por email deve usar índice do banco de dados
    Dado que o usuário está autenticado como "Employee"
    E que existem 5000 funcionários cadastrados no sistema
    Quando o usuário busca funcionário por email "joao@supply.com"
    Então o sistema deve retornar status 200
    E o tempo de resposta deve ser menor que 100ms
    E a busca deve utilizar índice no campo email