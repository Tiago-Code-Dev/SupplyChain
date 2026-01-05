using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Domain.Entities;

public class CustomRole : Entity
{
    public string Name { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public int HierarchyLevel { get; private set; }
    public bool IsSystemRole { get; private set; }
    public Role? LegacyRole { get; private set; }

    private readonly List<RolePermission> _permissions = new();
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private CustomRole() { }

    public static Result<CustomRole> Create(
        string name,
        string displayName,
        int hierarchyLevel,
        bool isSystemRole = false,
        Role? legacyRole = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<CustomRole>.Failure(Error.Validation("Name", "Name is required"));

        if (string.IsNullOrWhiteSpace(displayName))
            return Result<CustomRole>.Failure(Error.Validation("DisplayName", "Display name is required"));

        if (hierarchyLevel < 1 || hierarchyLevel > 100)
            return Result<CustomRole>.Failure(Error.Validation("HierarchyLevel", "Hierarchy level must be between 1 and 100"));

        return Result<CustomRole>.Success(new CustomRole
        {
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            HierarchyLevel = hierarchyLevel,
            IsSystemRole = isSystemRole,
            LegacyRole = legacyRole
        });
    }

    public static CustomRole CreateForSeed(
        Guid id,
        string name,
        string displayName,
        int hierarchyLevel,
        Role legacyRole)
    {
        return new CustomRole
        {
            Id = id,
            Name = name,
            DisplayName = displayName,
            HierarchyLevel = hierarchyLevel,
            IsSystemRole = true,
            LegacyRole = legacyRole
        };
    }

    public Result Update(string displayName, int hierarchyLevel)
    {
        if (IsSystemRole)
            return Result.Failure(Error.Validation("SystemRole", "Cannot modify system roles"));

        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure(Error.Validation("DisplayName", "Display name is required"));

        if (hierarchyLevel < 1 || hierarchyLevel > 100)
            return Result.Failure(Error.Validation("HierarchyLevel", "Hierarchy level must be between 1 and 100"));

        DisplayName = displayName;
        HierarchyLevel = hierarchyLevel;
        return Result.Success();
    }

    public bool CanManageRole(CustomRole targetRole)
    {
        return HierarchyLevel > targetRole.HierarchyLevel;
    }
}