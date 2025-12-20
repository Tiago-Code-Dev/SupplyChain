using Microsoft.AspNetCore.Identity;

namespace EmployeeManagement.Infrastructure.Identity;

/// <summary>
/// Role do sistema estendendo IdentityRole
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationRole() : base() { }
    
    public ApplicationRole(string roleName) : base(roleName) { }
    
    public ApplicationRole(string roleName, string description) : base(roleName)
    {
        Description = description;
    }
}

/// <summary>
/// Roles padrão do sistema
/// </summary>
public static class ApplicationRoles
{
    public const string Employee = "Employee";
    public const string Leader = "Leader";
    public const string Director = "Director";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Employee,
        Leader,
        Director
    };
}
