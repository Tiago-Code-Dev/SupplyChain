using Asp.Versioning;
using EmployeeManagement.Application.Features.Roles;
using EmployeeManagement.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.V1.Controllers;

/// <summary>
/// Controller para gerenciamento de roles/cargos
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Tags("Roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService, ILogger<RolesController> logger)
    {
        _roleService = roleService;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os cargos disponíveis ordenados por hierarquia
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllRolesAsync(cancellationToken);
        return Ok(roles.OrderByDescending(r => r.HierarchyLevel));
    }

    /// <summary>
    /// Obtém um cargo pelo ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roleService.GetByIdAsync(id, cancellationToken);
        if (role is null)
        {
            return NotFound(new { error = "Cargo não encontrado" });
        }
        return Ok(role);
    }

    /// <summary>
    /// Lista cargos disponíveis para selecionar como superior (para dropdown)
    /// </summary>
    [HttpGet("parents")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(IEnumerable<ParentRoleOption>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParentOptions(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllRolesAsync(cancellationToken);
        
        var options = roles
            .OrderByDescending(r => r.HierarchyLevel)
            .Select(r => new ParentRoleOption(r.Id, r.Name, r.DisplayName, r.HierarchyLevel))
            .ToList();

        return Ok(options);
    }

    /// <summary>
    /// Cria um novo cargo customizado (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Admin creating custom role: {Name}", request.Name);

            Result<RoleDto> result;

            if (request.HierarchyLevel.HasValue)
            {
                result = await _roleService.CreateCustomRoleAsync(
                    request.Name,
                    request.DisplayName,
                    request.HierarchyLevel.Value,
                    cancellationToken);
            }
            else if (request.ParentRoleId.HasValue)
            {
                result = await _roleService.CreateCustomRoleWithParentAsync(
                    request.Name,
                    request.DisplayName,
                    request.ParentRoleId.Value,
                    cancellationToken);
            }
            else
            {
                return BadRequest(new { error = "Informe o nível de hierarquia ou o cargo superior" });
            }

            if (result.IsFailure)
            {
                return result.Error.Code switch
                {
                    "Conflict" => Conflict(new { error = result.Error.Description }),
                    "NotFound" => NotFound(new { error = result.Error.Description }),
                    _ => BadRequest(new { error = result.Error.Description })
                };
            }

            _logger.LogInformation("Custom role created: {Name} at hierarchy level {Level}", 
                request.Name, result.Value.HierarchyLevel);

            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cargo {Name}", request.Name);
            return StatusCode(500, new { 
                error = ex.Message, 
                innerError = ex.InnerException?.Message 
            });
        }
    }

    /// <summary>
    /// Atualiza um cargo customizado (Admin only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin updating custom role: {RoleId}", id);

        var result = await _roleService.UpdateCustomRoleAsync(
            id,
            request.DisplayName,
            request.HierarchyLevel,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Description }),
                "Validation" => BadRequest(new { error = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Description })
            };
        }

        _logger.LogInformation("Custom role updated: {RoleId}", id);

        return Ok(result.Value);
    }

    /// <summary>
    /// Exclui um cargo customizado (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin deleting custom role: {RoleId}", id);

        var result = await _roleService.DeleteCustomRoleAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Description }),
                "Validation" => BadRequest(new { error = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Description })
            };
        }

        _logger.LogInformation("Custom role deleted: {RoleId}", id);

        return NoContent();
    }

    /// <summary>
    /// Visualiza a hierarquia de cargos
    /// </summary>
    [HttpGet("hierarchy")]
    [ProducesResponseType(typeof(HierarchyResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHierarchy(CancellationToken cancellationToken)
    {
        var roles = await _roleService.GetAllRolesAsync(cancellationToken);
        var rolesList = roles.OrderByDescending(r => r.HierarchyLevel).ToList();
        
        var items = rolesList.Select(r => new HierarchyItem(
            r.Id,
            r.Name,
            r.DisplayName,
            r.HierarchyLevel,
            r.IsSystemRole,
            rolesList.Where(x => x.HierarchyLevel < r.HierarchyLevel)
                     .Select(x => x.DisplayName)
                     .ToList()
        )).ToList();

        return Ok(new HierarchyResponse(items));
    }
}

#region DTOs

public record CreateRoleRequest(
    string Name,
    string DisplayName,
    Guid? ParentRoleId,
    int? HierarchyLevel);

public record UpdateRoleRequest(
    string DisplayName,
    int HierarchyLevel);

public record ParentRoleOption(
    Guid Id,
    string Name,
    string DisplayName,
    int HierarchyLevel);

public record HierarchyResponse(List<HierarchyItem> Roles);

public record HierarchyItem(
    Guid Id,
    string Name,
    string DisplayName,
    int HierarchyLevel,
    bool IsSystemRole,
    List<string> CanManage);

#endregion