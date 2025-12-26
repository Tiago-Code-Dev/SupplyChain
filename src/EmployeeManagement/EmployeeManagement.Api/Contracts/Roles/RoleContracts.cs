namespace EmployeeManagement.Api.Contracts.Roles;

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