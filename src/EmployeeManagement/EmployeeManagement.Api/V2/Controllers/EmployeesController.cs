using Asp.Versioning;
using EmployeeManagement.Api.Controllers;
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

namespace EmployeeManagement.Api.V2.Controllers;

/// <summary>
/// Controller para gerenciamento de funcionários - V2
/// </summary>
/// <remarks>
/// V2 apresenta DTOs simplificados e respostas mais consistentes.
/// </remarks>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[Authorize]
[Tags("Employees")]
public class EmployeesController : MainController
{
    public EmployeesController(ISender sender) : base(sender) { }

    /// <summary>
    /// Lista funcionários com paginação (V2)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseV2<EmployeeResponseV2>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] EmployeeQueryV2 query,
        CancellationToken cancellationToken)
    {
        var pageSize = Math.Min(query.Limit ?? 10, 50);
        var pageNumber = Math.Max((query.Offset ?? 0) / pageSize + 1, 1);

        var internalQuery = new GetAllEmployeesQuery(
            pageNumber,
            pageSize,
            query.Search,
            query.Name,
            query.Email,
            query.Role,
            query.ManagerId,
            query.SortBy,
            query.SortDesc ?? false);

        var result = await Sender.Send(internalQuery, cancellationToken);

        var response = new PagedResponseV2<EmployeeResponseV2>
        {
            Data = result.Items.Select(MapToV2).ToList(),
            Meta = new PaginationMetaV2
            {
                Total = result.TotalCount,
                Limit = pageSize,
                Offset = query.Offset ?? 0,
                HasMore = result.HasNextPage
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Busca funcionário por ID (V2)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDetailV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetEmployeeByIdQuery(id);
        var result = await Sender.Send(query, cancellationToken);
        
        if (result is null)
        {
            return NotFound(new { code = "EMPLOYEE_NOT_FOUND", message = "Funcionário não encontrado" });
        }

        return Ok(MapToDetailV2(result));
    }

    /// <summary>
    /// Cria novo funcionário (V2)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmployeeDetailV2), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequestV2 request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.Document,
            request.BirthDate,
            request.Password,
            request.Role,
            request.ManagerId,
            request.Phones,
            GetCurrentUserRole<Role>());

        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            employee => CreatedAtAction(nameof(GetById), new { id = employee.Id }, MapToDetailV2(employee)),
            error => HandleErrorV2(error));
    }

    /// <summary>
    /// Atualiza funcionário (V2)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDetailV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequestV2 request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.BirthDate,
            request.ManagerId,
            request.Phones,
            request.Role,
            GetCurrentUserRole<Role>());

        var result = await Sender.Send(command, cancellationToken);

        return result.Match(
            employee => Ok(MapToDetailV2(employee)),
            error => HandleErrorV2(error));
    }

    /// <summary>
    /// Remove funcionário (V2)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteEmployeeCommand(id, GetCurrentUserRole<Role>());
        var result = await Sender.Send(command, cancellationToken);

        return result.IsSuccess 
            ? NoContent() 
            : HandleErrorV2(result.Error);
    }

    #region Private Methods

    private static EmployeeResponseV2 MapToV2(EmployeeResponse employee)
    {
        return new EmployeeResponseV2
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Email = employee.Email,
            Role = employee.Role.ToString(),
            CreatedAt = employee.CreatedAt
        };
    }

    private static EmployeeDetailV2 MapToDetailV2(EmployeeResponse employee)
    {
        return new EmployeeDetailV2
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            FullName = employee.FullName,
            Email = employee.Email,
            Document = employee.DocumentNumber,
            BirthDate = employee.BirthDate,
            Role = employee.Role.ToString(),
            ManagerId = employee.ManagerId,
            ManagerName = employee.ManagerName,
            Phones = employee.PhoneNumbers.ToList(),
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    private IActionResult HandleErrorV2(Error error)
    {
        var statusCode = error.Code switch
        {
            _ when error.Code.EndsWith(".NotFound") => StatusCodes.Status404NotFound,
            _ when error.Code.EndsWith(".Conflict") => StatusCodes.Status409Conflict,
            _ when error.Code.Contains("Forbidden") => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(statusCode, new { code = error.Code, message = error.Description });
    }

    #endregion
}

#region V2 DTOs

/// <summary>
/// Query parameters para listagem V2
/// </summary>
public record EmployeeQueryV2
{
    public int? Limit { get; init; }
    public int? Offset { get; init; }
    public string? Search { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public Role? Role { get; init; }
    public Guid? ManagerId { get; init; }
    public string? SortBy { get; init; }
    public bool? SortDesc { get; init; }
}

/// <summary>
/// Response paginado V2
/// </summary>
public record PagedResponseV2<T>
{
    public required List<T> Data { get; init; }
    public required PaginationMetaV2 Meta { get; init; }
}

/// <summary>
/// Metadados de paginação V2
/// </summary>
public record PaginationMetaV2
{
    public int Total { get; init; }
    public int Limit { get; init; }
    public int Offset { get; init; }
    public bool HasMore { get; init; }
}

/// <summary>
/// Response de funcionário simplificado V2
/// </summary>
public record EmployeeResponseV2
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Role { get; init; }
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Response detalhado de funcionário V2
/// </summary>
public record EmployeeDetailV2
{
    public required Guid Id { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public required string Document { get; init; }
    public required DateTime BirthDate { get; init; }
    public required string Role { get; init; }
    public Guid? ManagerId { get; init; }
    public string? ManagerName { get; init; }
    public required IReadOnlyList<string> Phones { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>
/// Request de criação V2
/// </summary>
public record CreateEmployeeRequestV2(
    string FirstName,
    string LastName,
    string Email,
    string Document,
    DateTime BirthDate,
    string Password,
    Role Role,
    Guid? ManagerId,
    List<string> Phones);

/// <summary>
/// Request de atualização V2
/// </summary>
public record UpdateEmployeeRequestV2(
    string FirstName,
    string LastName,
    string Email,
    DateTime BirthDate,
    Guid? ManagerId,
    List<string> Phones,
    Role? Role = null);

#endregion