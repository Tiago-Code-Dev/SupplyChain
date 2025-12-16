using Asp.Versioning;
using EmployeeManagement.Api.Contracts;
using EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;
using EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;
using EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// Controller para gerenciamento de funcionários
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")] // Mantém rota legada para compatibilidade
[ApiVersion("1.0")]
[Authorize]
[Tags("Employees")]
public class EmployeesController : MainController
{
    public EmployeesController(ISender sender) : base(sender) { }

    /// <summary>
    /// Lista todos os funcionários com paginação e filtros
    /// </summary>
    /// <remarks>
    /// Retorna uma lista paginada com todos os funcionários cadastrados no sistema.
    /// Requer autenticação JWT.
    /// 
    /// **Filtros disponíveis:**
    /// - **searchTerm**: Busca genérica em nome, sobrenome, email e documento
    /// - **filterByName**: Filtro específico por nome (FirstName ou LastName)
    /// - **filterByEmail**: Filtro específico por email (busca parcial)
    /// - **filterByRole**: Filtro específico por permissão (Employee=1, Leader=2, Director=3, Admin=4)
    /// - **filterByManagerId**: Filtro específico por ID do gestor
    /// 
    /// **Ordenação:**
    /// - sortBy: firstname, lastname, email, role, createdat
    /// - sortDescending: true/false
    /// </remarks>
    /// <param name="pageNumber">Número da página (padrão: 1)</param>
    /// <param name="pageSize">Tamanho da página (padrão: 10, máximo: 50)</param>
    /// <param name="searchTerm">Termo de busca genérico (nome, email, documento)</param>
    /// <param name="filterByName">Filtro específico por nome</param>
    /// <param name="filterByEmail">Filtro específico por email</param>
    /// <param name="filterByRole">Filtro específico por permissão/role</param>
    /// <param name="filterByManagerId">Filtro específico por ID do gestor</param>
    /// <param name="sortBy">Campo para ordenação</param>
    /// <param name="sortDescending">Ordenar de forma decrescente?</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Lista paginada de funcionários</returns>
    /// <response code="200">Lista de funcionários retornada com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? filterByName = null,
        [FromQuery] string? filterByEmail = null,
        [FromQuery] Role? filterByRole = null,
        [FromQuery] Guid? filterByManagerId = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        pageSize = Math.Min(pageSize, 50);
        pageNumber = Math.Max(pageNumber, 1);

        var query = new GetAllEmployeesQuery(
            pageNumber,
            pageSize,
            searchTerm,
            filterByName,
            filterByEmail,
            filterByRole,
            filterByManagerId,
            sortBy,
            sortDescending);

        var result = await Sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Busca um funcionário pelo ID
    /// </summary>
    /// <remarks>
    /// Retorna os dados completos de um funcionário específico.
    /// </remarks>
    /// <param name="id">ID único do funcionário (GUID)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dados do funcionário</returns>
    /// <response code="200">Funcionário encontrado</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="404">Funcionário não encontrado</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetEmployeeByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Cria um novo funcionário
    /// </summary>
    /// <remarks>
    /// Cadastra um novo funcionário no sistema.
    /// 
    /// **Regras de negócio:**
    /// - Não é permitido criar um usuário com permissões maiores que as do usuário atual
    /// - O funcionário deve ter pelo menos 18 anos
    /// - Email e documento devem ser únicos
    /// - A senha deve conter: mínimo 8 caracteres, letra maiúscula, minúscula, número e caractere especial
    /// 
    /// **Roles disponíveis:** Employee (1), Leader (2), Director (3)
    /// </remarks>
    /// <param name="request">Dados do funcionário a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Funcionário criado</returns>
    /// <response code="201">Funcionário criado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="403">Sem permissão para criar funcionário com esta role</response>
    /// <response code="409">Email ou documento já existente</response>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.DocumentNumber,
            request.BirthDate,
            request.Password,
            request.Role,
            request.ManagerId,
            request.PhoneNumbers,
            GetCurrentUserRole<Role>());

        var result = await Sender.Send(command, cancellationToken);

        return HandleCreatedResult(result, nameof(GetById), e => new { id = e.Id });
    }

    /// <summary>
    /// Atualiza um funcionário existente
    /// </summary>
    /// <remarks>
    /// Atualiza os dados de um funcionário.
    /// 
    /// **Campos atualizáveis:**
    /// - Nome e sobrenome
    /// - Email (deve ser único)
    /// - Data de nascimento (deve ter 18+ anos)
    /// - Telefones
    /// - Gerente
    /// - Role (respeitando hierarquia)
    /// 
    /// **Nota:** Documento não pode ser alterado.
    /// </remarks>
    /// <param name="id">ID único do funcionário (GUID)</param>
    /// <param name="request">Dados atualizados do funcionário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Funcionário atualizado</returns>
    /// <response code="200">Funcionário atualizado com sucesso</response>
    /// <response code="400">Dados inválidos</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="403">Sem permissão para atualizar funcionário com esta role</response>
    /// <response code="404">Funcionário não encontrado</response>
    /// <response code="409">Email já utilizado por outro funcionário</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.BirthDate,
            request.ManagerId,
            request.PhoneNumbers,
            request.Role,
            GetCurrentUserRole<Role>());

        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Exclui um funcionário (soft delete)
    /// </summary>
    /// <remarks>
    /// Remove um funcionário do sistema, marcando-o como excluído.
    /// O registro do funcionário permanece no banco de dados para fins de auditoria.
    /// 
    /// **Regras de negócio:**
    /// - Apenas Leader e Director podem excluir funcionários
    /// - Não é possível excluir funcionário que possui subordinados
    /// </remarks>
    /// <param name="id">ID único do funcionário (GUID)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <response code="204">Funcionário excluído com sucesso</response>
    /// <response code="401">Usuário não autenticado</response>
    /// <response code="403">Sem permissão para excluir funcionários</response>
    /// <response code="404">Funcionário não encontrado</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteEmployeeCommand(id, GetCurrentUserRole<Role>());
        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }
}