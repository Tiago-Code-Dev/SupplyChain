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
    /// Obtém um role pelo ID
    /// </summary>
    Task<RoleDto?> GetByIdAsync(Guid roleId, CancellationToken ct = default);
    
    /// <summary>
    /// Lista todos os roles (sistema + customizados)
    /// </summary>
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Cria um novo role customizado com hierarquia baseada no cargo superior
    /// </summary>
    Task<Result<RoleDto>> CreateCustomRoleWithParentAsync(
        string name, 
        string displayName, 
        Guid parentRoleId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Cria um novo role customizado com nível de hierarquia específico
    /// </summary>
    Task<Result<RoleDto>> CreateCustomRoleAsync(
        string name, 
        string displayName, 
        int hierarchyLevel,
        CancellationToken ct = default);
    
    /// <summary>
    /// Atualiza um cargo customizado (não permite editar cargos do sistema)
    /// </summary>
    Task<Result<RoleDto>> UpdateCustomRoleAsync(
        Guid roleId,
        string displayName, 
        int hierarchyLevel,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exclui um cargo customizado (não permite excluir cargos do sistema)
    /// </summary>
    Task<Result> DeleteCustomRoleAsync(Guid roleId, CancellationToken ct = default);
    
    /// <summary>
    /// Verifica se um role pode gerenciar outro
    /// </summary>
    bool CanManage(Role currentRole, Role targetRole);
}