using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Application.Interfaces;

/// <summary>
/// Interface para serviço de Identity
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Cria um novo usuário
    /// </summary>
    Task<Result<Guid>> CreateUserAsync(
        string email, 
        string password, 
        string firstName, 
        string lastName,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Autentica um usuário e retorna tokens
    /// </summary>
    Task<Result<AuthResult>> AuthenticateAsync(
        string email, 
        string password,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renova o token usando refresh token
    /// </summary>
    Task<Result<AuthResult>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga um refresh token
    /// </summary>
    Task<Result> RevokeTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoga todos os tokens de um usuário
    /// </summary>
    Task<Result> RevokeAllUserTokensAsync(
        Guid userId,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona role ao usuário
    /// </summary>
    Task<Result> AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove role do usuário
    /// </summary>
    Task<Result> RemoveFromRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém roles do usuário
    /// </summary>
    Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se usuário está em uma role
    /// </summary>
    Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona claim ao usuário
    /// </summary>
    Task<Result> AddClaimAsync(Guid userId, string claimType, string claimValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém claims do usuário
    /// </summary>
    Task<IDictionary<string, string>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove claim do usuário
    /// </summary>
    Task<Result> RemoveClaimAsync(Guid userId, string claimType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Altera senha do usuário
    /// </summary>
    Task<Result> ChangePasswordAsync(
        Guid userId, 
        string currentPassword, 
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gera token para reset de senha
    /// </summary>
    Task<Result<string>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reseta senha usando token
    /// </summary>
    Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deleta usuário (soft delete)
    /// </summary>
    Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuário por email
    /// </summary>
    Task<UserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca usuário por ID
    /// </summary>
    Task<UserInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado de autenticação com Access Token e Refresh Token
/// </summary>
public record AuthResult(
    Guid UserId,
    string Email,
    string FullName,
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    IList<string> Roles);

/// <summary>
/// Informações do usuário
/// </summary>
public record UserInfo(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    Guid? EmployeeId,
    bool IsActive,
    IList<string> Roles);
