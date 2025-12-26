using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Features.Roles;

/// <summary>
/// DTO para representar um Role
/// </summary>
public record RoleDto(
    Guid Id,
    string Name,
    string DisplayName,
    int HierarchyLevel,
    bool IsSystemRole);

public interface IRoleService
{
    /// <summary>
    /// Obtém um role pelo enum legado
    /// </summary>
    Task<RoleDto?> GetByLegacyRoleAsync(Role role, CancellationToken ct = default);
    
    /// <summary>
    /// Lista todos os roles (sistema + customizados)
    /// </summary>
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Cria um novo role customizado (via frontend)
    /// </summary>
    Task<Result<RoleDto>> CreateCustomRoleAsync(
        string name, 
        string displayName, 
        int hierarchyLevel,
        CancellationToken ct = default);
    
    /// <summary>
    /// Verifica se um role pode gerenciar outro (compatibilidade com enum legado)
    /// </summary>
    bool CanManage(Role currentRole, Role targetRole);
}