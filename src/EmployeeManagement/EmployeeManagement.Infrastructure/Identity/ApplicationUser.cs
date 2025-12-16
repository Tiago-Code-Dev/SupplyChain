using Microsoft.AspNetCore.Identity;

namespace EmployeeManagement.Infrastructure.Identity;

/// <summary>
/// Usuário do sistema estendendo IdentityUser
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}";
    
    /// <summary>
    /// ID do Employee associado (se houver)
    /// </summary>
    public Guid? EmployeeId { get; set; }
    
    /// <summary>
    /// Data de criação
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Usuário ativo
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Último login
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
}
