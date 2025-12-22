using EmployeeManagement.Application.Features.Roles;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;

namespace EmployeeManagement.Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly ICustomRoleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RoleService(ICustomRoleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RoleDto?> GetByLegacyRoleAsync(Role role, CancellationToken ct = default)
    {
        var customRole = await _repository.GetByLegacyRoleAsync(role, ct);
        return customRole is null ? null : ToDto(customRole);
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(CancellationToken ct = default)
    {
        var roles = await _repository.GetAllAsync(ct);
        return roles.Select(ToDto).ToList();
    }

    public async Task<Result<RoleDto>> CreateCustomRoleAsync(
        string name,
        string displayName,
        int hierarchyLevel,
        CancellationToken ct = default)
    {
        if (await _repository.NameExistsAsync(name, null, ct))
        {
            return Result<RoleDto>.Failure(
                Error.Conflict("CustomRole", $"Role with name '{name}' already exists"));
        }

        var roleResult = CustomRole.Create(name, displayName, hierarchyLevel, isSystemRole: false);
        if (roleResult.IsFailure)
            return Result<RoleDto>.Failure(roleResult.Error);

        await _repository.AddAsync(roleResult.Value, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<RoleDto>.Success(ToDto(roleResult.Value));
    }

    public bool CanManage(Role currentRole, Role targetRole)
    {
        return currentRole > targetRole;
    }

    private static RoleDto ToDto(CustomRole role) => new(
        role.Id,
        role.Name,
        role.DisplayName,
        role.HierarchyLevel,
        role.IsSystemRole);
}