using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Entities;

public class RolePermission : Entity
{
    public Guid CustomRoleId { get; private set; }
    public string Permission { get; private set; } = default!;
    public string? Resource { get; private set; }

    public CustomRole CustomRole { get; private set; } = default!;

    private RolePermission() { }

    public static RolePermission Create(Guid customRoleId, string permission, string? resource = null)
    {
        return new RolePermission
        {
            CustomRoleId = customRoleId,
            Permission = permission,
            Resource = resource
        };
    }
}