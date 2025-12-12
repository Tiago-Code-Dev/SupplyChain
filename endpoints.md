### Resumo dos Endpoints:

### Recurso	Endpoint	Método	Descrição

# Auth	/api/auth/login	POST	Autenticar usuário e obter token.

# Role	/api/role	GET	Listar todos os cargos.
	/api/role/{id}	GET	Obter detalhes de um cargo.
	/api/role	POST	Criar um novo cargo.
	/api/role/{id}	PUT	Atualizar dados de um cargo.
	/api/role/{id}	DELETE	Deletar um cargo.
# EmployeeRole	/api/employeerole	GET	Listar todos os cargos atribuídos.
	/api/employeerole	POST	Atribuir um cargo a um empregado.
	/api/employeerole/{employeeId}/{roleId}	DELETE	Remover cargo de um empregado.
# Log	/api/log	GET	Listar todos os logs de ações.
	/api/log/{id}	GET	Obter detalhes de um log.
# Audit	/api/audit	GET	Listar todas as auditorias.
	/api/audit/{id}	GET	Obter detalhes de uma auditoria.