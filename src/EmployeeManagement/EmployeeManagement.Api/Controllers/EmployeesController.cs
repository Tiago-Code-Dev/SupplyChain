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
[Authorize]
[Tags("Employees")]
public class EmployeesController : MainController
{
    public EmployeesController(ISender sender) : base(sender) { }

    /// <summary>
    /// Lista todos os funcionários com paginação
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
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
            sortBy,
            sortDescending);

        var result = await Sender.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Busca um funcionário pelo ID
    /// </summary>
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
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
            request.PhoneNumbers);

        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Exclui um funcionário (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteEmployeeCommand(id);
        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }
}