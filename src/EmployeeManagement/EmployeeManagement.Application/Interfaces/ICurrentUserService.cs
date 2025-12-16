namespace EmployeeManagement.Application.Interfaces;

/// <summary>
/// Interface para obter informações do usuário atual autenticado
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// ID do usuário atual (do token JWT)
    /// </summary>
    Guid? UserId { get; }
    
    /// <summary>
    /// Email do usuário atual
    /// </summary>
    string? Email { get; }
    
    /// <summary>
    /// Role do usuário atual
    /// </summary>
    string? Role { get; }
    
    /// <summary>
    /// Indica se o usuário está autenticado
    /// </summary>
    bool IsAuthenticated { get; }
}
