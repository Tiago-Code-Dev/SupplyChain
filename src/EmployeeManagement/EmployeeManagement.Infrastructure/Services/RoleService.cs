using EmployeeManagement.Application.Features.Roles;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly ICustomRoleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoleService> _logger;

    public RoleService(ICustomRoleRepository repository, IUnitOfWork unitOfWork, ILogger<RoleService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<RoleDto?> GetByLegacyRoleAsync(Role role, CancellationToken ct = default)
    {
        var customRole = await _repository.GetByLegacyRoleAsync(role, ct);
        return customRole is null ? null : ToDto(customRole);
    }

    public async Task<RoleDto?> GetByIdAsync(Guid roleId, CancellationToken ct = default)
    {
        var customRole = await _repository.GetByIdAsync(roleId, ct);
        return customRole is null ? null : ToDto(customRole);
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
        var roles = await _repository.GetAllAsync(ct);
        return roles.Select(ToDto).ToList();
    }

    public async Task<Result<RoleDto>> CreateCustomRoleWithParentAsync(
        string name,
        string displayName,
        Guid parentRoleId,
        CancellationToken ct = default)
    {
        if (await _repository.NameExistsAsync(name, null, ct))
        {
            return Result<RoleDto>.Failure(
                Error.Conflict("Name", $"Já existe um cargo com o nome '{name}'"));
        }

        var parentRole = await _repository.GetByIdAsync(parentRoleId, ct);
        if (parentRole is null)
        {
            return Result<RoleDto>.Failure(
                Error.NotFound("ParentRole", "Cargo superior não encontrado"));
        }

        var allRoles = await _repository.GetAllAsync(ct);
        var hierarchyLevel = CalculateHierarchyLevel(parentRole.HierarchyLevel, allRoles);

        var roleResult = CustomRole.Create(name, displayName, hierarchyLevel, isSystemRole: false);
        if (roleResult.IsFailure)
            return Result<RoleDto>.Failure(roleResult.Error);

        await _repository.AddAsync(roleResult.Value, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RoleDto>.Success(ToDto(roleResult.Value));
    }

    public async Task<Result<RoleDto>> CreateCustomRoleAsync(
        string name,
        string displayName,
        int hierarchyLevel,
        CancellationToken ct = default)
    {
        var nameExists = await _repository.NameExistsAsync(name, null, ct);
        _logger.LogInformation("CreateCustomRoleAsync: Name={Name}, NameExists={NameExists}", name, nameExists);

        if (nameExists)
        {
            _logger.LogWarning("CreateCustomRoleAsync: Cargo com nome '{Name}' já existe", name);
            return Result<RoleDto>.Failure(
                Error.Conflict("Name", $"Já existe um cargo com o nome '{name}'"));
        }

        var roleResult = CustomRole.Create(name, displayName, hierarchyLevel, isSystemRole: false);
        if (roleResult.IsFailure)
            return Result<RoleDto>.Failure(roleResult.Error);

        await _repository.AddAsync(roleResult.Value, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("CreateCustomRoleAsync: Cargo '{Name}' criado com sucesso, Id={Id}", name, roleResult.Value.Id);
        return Result<RoleDto>.Success(ToDto(roleResult.Value));
    }

    public async Task<Result<RoleDto>> UpdateCustomRoleAsync(
        Guid roleId,
        string displayName,
        int hierarchyLevel,
        CancellationToken ct = default)
    {
        var role = await _repository.GetByIdAsync(roleId, ct);
        
        if (role is null)
        {
            return Result<RoleDto>.Failure(Error.NotFound("Role", "Cargo não encontrado"));
        }

        var updateResult = role.Update(displayName, hierarchyLevel);
        if (updateResult.IsFailure)
        {
            return Result<RoleDto>.Failure(updateResult.Error);
        }

        await _repository.UpdateAsync(role, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RoleDto>.Success(ToDto(role));
    }

    public async Task<Result> DeleteCustomRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _repository.GetByIdAsync(roleId, ct);
        
        if (role is null)
        {
            return Result.Failure(Error.NotFound("Role", "Cargo não encontrado"));
        }

        if (role.IsSystemRole)
        {
            return Result.Failure(Error.Validation("Role", "Não é permitido excluir cargos do sistema"));
        }

        await _repository.DeleteAsync(roleId, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }

    public bool CanManage(Role currentRole, Role targetRole)
    {
        return currentRole > targetRole;
    }

    private static int CalculateHierarchyLevel(int parentLevel, IEnumerable<CustomRole> allRoles)
    {
        var existingLevels = allRoles.Select(r => r.HierarchyLevel).ToHashSet();
        var newLevel = parentLevel - 2;
        
        while (existingLevels.Contains(newLevel) && newLevel > 1)
        {
            newLevel -= 2;
        }
        
        if (existingLevels.Contains(newLevel))
        {
            newLevel = parentLevel - 1;
            while (existingLevels.Contains(newLevel) && newLevel > 1)
            {
                newLevel--;
            }
        }
        
        return Math.Max(1, newLevel);
    }

    private static RoleDto ToDto(CustomRole role) => new(
        role.Id,
        role.Name,
        role.DisplayName,
        role.HierarchyLevel,
        role.IsSystemRole);
}