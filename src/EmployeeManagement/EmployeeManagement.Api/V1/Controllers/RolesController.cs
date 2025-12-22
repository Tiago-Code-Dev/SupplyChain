using Asp.Versioning;
using EmployeeManagement.Api.Controllers;
using EmployeeManagement.Application.Features.Roles;
using EmployeeManagement.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.V1.Controllers;

/// <summary>
/// Gerenciamento de Roles customizados
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "RequireAdmin")]
public class RolesController : MainController
{
    private readonly IRoleService _roleService;
    private readonly ICustomRoleRepository _repository;

    public RolesController(
        ISender sender,
        IRoleService roleService,
        ICustomRoleRepository repository) : base(sender)
    {
        _roleService = roleService;
        _repository = repository;
    }

    /// <summary>
    /// Lista todos os roles (sistema + customizados)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllRolesAsync(cancellationToken);
        
        var response = roles.Select(r => new RoleResponse(
            r.Id,
            r.Name,
            r.DisplayName,
            r.HierarchyLevel,
            r.IsSystemRole));

        return Ok(response);
    }

    /// <summary>
    /// Obtém um role pelo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (role is null)
            return NotFound();

        return Ok(new RoleResponse(
            role.Id,
            role.Name,
            role.DisplayName,
            role.HierarchyLevel,
            role.IsSystemRole));
    }

    /// <summary>
    /// Cria um novo role customizado
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _roleService.CreateCustomRoleAsync(
            request.Name,
            request.DisplayName,
            request.HierarchyLevel,
            cancellationToken);

        return result.Match(
            role => CreatedAtAction(
                nameof(GetById),
                new { id = role.Id },
                new RoleResponse(role.Id, role.Name, role.DisplayName, role.HierarchyLevel, role.IsSystemRole)),
            error => HandleError(error));
    }

    /// <summary>
    /// Atualiza um role customizado (roles do sistema não podem ser alterados)
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (role is null)
            return NotFound();

        var updateResult = role.Update(request.DisplayName, request.HierarchyLevel);
        
        if (updateResult.IsFailure)
            return HandleError(updateResult.Error);

        await _repository.UpdateAsync(role, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Remove um role customizado (roles do sistema não podem ser removidos)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var role = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (role is null)
            return NotFound();

        if (role.IsSystemRole)
            return Forbid();

        await _repository.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}

// DTOs
public sealed record CreateRoleRequest(
    string Name,
    string DisplayName,
    int HierarchyLevel);

public sealed record UpdateRoleRequest(
    string DisplayName,
    int HierarchyLevel);

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string DisplayName,
    int HierarchyLevel,
    bool IsSystemRole);