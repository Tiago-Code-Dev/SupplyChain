using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Infrastructure.Identity;

/// <summary>
/// Implementação do serviço de Identity com suporte a Refresh Tokens e Token Rotation
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AppIdentityDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<IdentityService> _logger;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager,
        AppIdentityDbContext context,
        IOptions<JwtSettings> jwtSettings,
        ILogger<IdentityService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    #region User Management

    public async Task<Result<Guid>> CreateUserAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        Guid? employeeId = null,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            EmployeeId = employeeId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to create user {Email}: {Errors}", email, errors);
            return Result<Guid>.Failure(Error.Validation("User", errors));
        }

        _logger.LogInformation("User created successfully: {Email} ({UserId})", email, user.Id);
        return Result<Guid>.Success(user.Id);
    }

    public async Task<Result> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User", AuthMessages.UserNotFound));
        }

        // Soft delete
        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        // Revogar todos os tokens
        await RevokeAllUserTokensAsync(userId, null, "User deleted", cancellationToken);

        _logger.LogInformation("User {UserId} deleted (soft)", userId);
        return Result.Success();
    }

    public async Task<UserInfo?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserInfo(user, roles);
    }

    public async Task<UserInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserInfo(user, roles);
    }

    #endregion

    #region Authentication

    public async Task<Result<AuthResult>> AuthenticateAsync(
        string email,
        string password,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Authentication failed: User {Email} not found. IP: {IpAddress}", email, ipAddress);
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.InvalidCredentials));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Authentication failed: User {Email} is inactive. IP: {IpAddress}", email, ipAddress);
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.UserInactive));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User {Email} is locked out. IP: {IpAddress}", email, ipAddress);
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.AccountLocked));
        }

        if (!result.Succeeded)
        {
            _logger.LogWarning("Authentication failed: Invalid password for {Email}. IP: {IpAddress}", email, ipAddress);
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.InvalidCredentials));
        }

        // Atualizar último login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Gerar tokens
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        var accessToken = GenerateAccessToken(user, roles, claims);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ipAddress, cancellationToken);

        var authResult = new AuthResult(
            user.Id,
            user.Email!,
            user.FullName,
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            roles);

        _logger.LogInformation("User {Email} authenticated successfully. IP: {IpAddress}", email, ipAddress);
        return Result<AuthResult>.Success(authResult);
    }

    public async Task<Result<AuthResult>> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token == null)
        {
            _logger.LogWarning("Refresh token not found. IP: {IpAddress}", ipAddress);
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.InvalidRefreshToken));
        }

        if (!token.IsActive)
        {
            _logger.LogWarning("Refresh token is not active for user {UserId}. IP: {IpAddress}", token.UserId, ipAddress);
            
            // Se o token foi usado após ser revogado, revogar todos os tokens descendentes (possível ataque)
            if (token.IsRevoked)
            {
                await RevokeDescendantTokensAsync(token, ipAddress, "Attempted reuse of revoked token", cancellationToken);
            }
            
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.InvalidRefreshToken));
        }

        var user = token.User;
        if (!user.IsActive)
        {
            return Result<AuthResult>.Failure(Error.Unauthorized(AuthMessages.UserInactive));
        }

        // Rotacionar refresh token (revogar o antigo e criar novo)
        var newRefreshToken = await RotateRefreshTokenAsync(token, ipAddress, cancellationToken);

        // Gerar novo access token
        var roles = await _userManager.GetRolesAsync(user);
        var claims = await _userManager.GetClaimsAsync(user);
        var accessToken = GenerateAccessToken(user, roles, claims);

        var authResult = new AuthResult(
            user.Id,
            user.Email!,
            user.FullName,
            accessToken.Token,
            accessToken.ExpiresAt,
            newRefreshToken.Token,
            newRefreshToken.ExpiresAt,
            roles);

        _logger.LogInformation("Token refreshed for user {UserId}. IP: {IpAddress}", user.Id, ipAddress);
        return Result<AuthResult>.Success(authResult);
    }

    public async Task<Result> RevokeTokenAsync(
        string refreshToken,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == refreshToken, cancellationToken);

        if (token == null)
        {
            return Result.Failure(Error.NotFound("RefreshToken", AuthMessages.InvalidRefreshToken));
        }

        if (!token.IsActive)
        {
            return Result.Failure(Error.Validation("RefreshToken", AuthMessages.TokenRevoked));
        }

        token.Revoke(ipAddress, reason ?? "Revoked manually");
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token revoked for user {UserId}. Reason: {Reason}. IP: {IpAddress}", 
            token.UserId, reason, ipAddress);
        
        return Result.Success();
    }

    public async Task<Result> RevokeAllUserTokensAsync(
        Guid userId,
        string? ipAddress = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(ipAddress, reason ?? "All tokens revoked");
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("All refresh tokens revoked for user {UserId}. Count: {Count}. IP: {IpAddress}", 
            userId, tokens.Count, ipAddress);
        
        return Result.Success();
    }

    #endregion

    #region Password Management

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User", userId));
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Password", errors));
        }

        // Revogar todos os refresh tokens após mudança de senha
        await RevokeAllUserTokensAsync(userId, null, "Password changed", cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}", userId);
        return Result.Success();
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            // Não revelar se o usuário existe ou não (segurança)
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return Result<string>.Success(string.Empty);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        _logger.LogInformation("Password reset token generated for user {Email}", email);
        return Result<string>.Success(token);
    }

    public async Task<Result> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return Result.Failure(Error.Validation("Token", "Invalid or expired token"));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Password", errors));
        }

        // Revogar todos os refresh tokens após reset de senha
        await RevokeAllUserTokensAsync(user.Id, null, "Password reset", cancellationToken);

        _logger.LogInformation("Password reset for user {Email}", email);
        return Result.Success();
    }

    #endregion

    #region Roles & Claims

    public async Task<Result> AddToRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User", userId));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new ApplicationRole(role));
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Role", errors));
        }

        _logger.LogInformation("User {UserId} added to role {Role}", userId, role);
        return Result.Success();
    }

    public async Task<Result> RemoveFromRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User", userId));
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Role", errors));
        }

        _logger.LogInformation("User {UserId} removed from role {Role}", userId, role);
        return Result.Success();
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user == null ? new List<string>() : await _userManager.GetRolesAsync(user);
    }

    public async Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user != null && await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<Result> AddClaimAsync(Guid userId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User", userId));
        }

        var claim = new Claim(claimType, claimValue);
        var result = await _userManager.AddClaimAsync(user, claim);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Claim", errors));
        }

        _logger.LogInformation("Claim {ClaimType}={ClaimValue} added to user {UserId}", claimType, claimValue, userId);
        return Result.Success();
    }

    public async Task<IDictionary<string, string>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new Dictionary<string, string>();
        }

        var claims = await _userManager.GetClaimsAsync(user);
        return claims.ToDictionary(c => c.Type, c => c.Value);
    }

    public async Task<Result> RemoveClaimAsync(Guid userId, string claimType, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User", userId));
        }

        var claims = await _userManager.GetClaimsAsync(user);
        var claimToRemove = claims.FirstOrDefault(c => c.Type == claimType);

        if (claimToRemove == null)
        {
            return Result.Failure(Error.NotFound("Claim", $"Claim '{claimType}' not found"));
        }

        var result = await _userManager.RemoveClaimAsync(user, claimToRemove);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(Error.Validation("Claim", errors));
        }

        _logger.LogInformation("Claim {ClaimType} removed from user {UserId}", claimType, userId);
        return Result.Success();
    }

    #endregion

    #region Private Methods

    private static UserInfo MapToUserInfo(ApplicationUser user, IList<string> roles) => new(
        user.Id,
        user.Email!,
        user.FirstName,
        user.LastName,
        user.FullName,
        user.EmployeeId,
        user.IsActive,
        roles);

    private (string Token, DateTime ExpiresAt) GenerateAccessToken(
        ApplicationUser user,
        IList<string> roles,
        IList<Claim> userClaims)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("employee_id", user.EmployeeId?.ToString() ?? "")
        };

        // Adicionar roles como claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Adicionar claims customizadas
        claims.AddRange(userClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(
        Guid userId, 
        string? ipAddress, 
        CancellationToken cancellationToken)
    {
        var refreshToken = RefreshToken.Create(userId, _jwtSettings.RefreshTokenExpirationDays, ipAddress);
        
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Limpar tokens antigos do usuário
        await CleanupOldRefreshTokensAsync(userId, cancellationToken);

        return refreshToken;
    }

    private async Task<RefreshToken> RotateRefreshTokenAsync(
        RefreshToken oldToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var newToken = RefreshToken.Create(oldToken.UserId, _jwtSettings.RefreshTokenExpirationDays, ipAddress);
        
        oldToken.Revoke(ipAddress, "Replaced by new token", newToken.Token);
        
        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync(cancellationToken);

        return newToken;
    }

    private async Task RevokeDescendantTokensAsync(
        RefreshToken token,
        string? ipAddress,
        string reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token.ReplacedByToken)) return;

        var childToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == token.ReplacedByToken, cancellationToken);

        if (childToken == null) return;

        if (childToken.IsActive)
        {
            childToken.Revoke(ipAddress, reason);
        }

        await RevokeDescendantTokensAsync(childToken, ipAddress, reason, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task CleanupOldRefreshTokensAsync(Guid userId, CancellationToken cancellationToken)
    {
        // Remover tokens expirados há mais de 7 dias
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        
        var oldTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && 
                       (t.ExpiresAt < cutoffDate || 
                        (t.RevokedAt != null && t.RevokedAt < cutoffDate)))
            .ToListAsync(cancellationToken);

        if (oldTokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(oldTokens);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion
}
