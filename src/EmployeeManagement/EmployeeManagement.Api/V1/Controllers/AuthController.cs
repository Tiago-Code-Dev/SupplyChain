using System.Security.Claims;
using Asp.Versioning;
using EmployeeManagement.Api.Configurations;
using EmployeeManagement.Application.Features.Roles;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmployeeManagement.Api.V1.Controllers;

/// <summary>
/// Controller para autenticação usando Identity com suporte a Refresh Token
/// </summary>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IIdentityService identityService, 
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _identityService = identityService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login no sistema
    /// </summary>
    /// <remarks>
    /// Autentica o usuário e retorna access token + refresh token.
    /// 
    /// **Credenciais padrão:**
    /// - Email: admin@empresa.com
    /// - Senha: Admin@123
    /// 
    /// **Tokens:**
    /// - Access Token: válido por 15 minutos
    /// - Refresh Token: válido por 7 dias
    /// 
    /// **Rate Limiting:**
    /// - Máximo 5 tentativas por minuto por IP
    /// </remarks>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        _logger.LogInformation("Login attempt from IP: {IpAddress} for email: {Email}", ipAddress, request.Email);
        
        var result = await _identityService.AuthenticateAsync(request.Email, request.Password, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Login failed from IP: {IpAddress} for email: {Email}", ipAddress, request.Email);
            return Unauthorized(new { error = result.Error.Description });
        }

        var authResult = result.Value;
        return Ok(new AuthResponse(
            authResult.AccessToken,
            authResult.AccessTokenExpiresAt,
            authResult.RefreshToken,
            authResult.RefreshTokenExpiresAt,
            new UserResponse(
                authResult.UserId,
                authResult.Email,
                authResult.FullName,
                authResult.Roles.ToList())));
    }

    /// <summary>
    /// Renova o access token usando refresh token
    /// </summary>
    /// <remarks>
    /// Usa o refresh token para obter novos tokens.
    /// O refresh token antigo é revogado e um novo é gerado (rotation).
    /// 
    /// **Rate Limiting:**
    /// - Máximo 5 tentativas por minuto por IP
    /// </remarks>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _identityService.RefreshTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(new { error = result.Error.Description });
        }

        var authResult = result.Value;
        return Ok(new AuthResponse(
            authResult.AccessToken,
            authResult.AccessTokenExpiresAt,
            authResult.RefreshToken,
            authResult.RefreshTokenExpiresAt,
            new UserResponse(
                authResult.UserId,
                authResult.Email,
                authResult.FullName,
                authResult.Roles.ToList())));
    }

    /// <summary>
    /// Revoga um refresh token (logout)
    /// </summary>
    /// <remarks>
    /// Invalida o refresh token fornecido.
    /// Use para implementar logout ou invalidar sessões específicas.
    /// </remarks>
    [HttpPost("revoke-token")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _identityService.RevokeTokenAsync(request.RefreshToken, ipAddress, "User logout", cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>
    /// Revoga todos os tokens do usuário atual (logout de todas sessões)
    /// </summary>
    [HttpPost("revoke-all-tokens")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAllTokens(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var ipAddress = GetIpAddress();
        await _identityService.RevokeAllUserTokensAsync(userId.Value, ipAddress, "User revoked all sessions", cancellationToken);

            return NoContent();
        }

        /// <summary>
        /// Solicita reset de senha
        /// </summary>
        /// <remarks>
        /// Gera um token para reset de senha e envia por email.
        /// Por segurança, sempre retorna sucesso mesmo se o email não existir.
        /// </remarks>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            var result = await _identityService.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            {
                // Obter nome do usuário para personalizar o email
                var user = await _identityService.GetUserByEmailAsync(request.Email, cancellationToken);
                var userName = user?.FullName ?? request.Email;

                // Enviar email com o token
                var emailSent = await _emailService.SendPasswordResetEmailAsync(
                    request.Email, 
                    userName, 
                    result.Value, 
                    cancellationToken: cancellationToken);

                if (emailSent)
                {
                    _logger.LogInformation("Password reset email sent to {Email}", request.Email);
                }
                else
                {
                    _logger.LogWarning("Failed to send password reset email to {Email}", request.Email);
                }
            }

            return Ok(new { message = "Se o email existir em nossa base, você receberá um link para redefinir sua senha." });
        }

        /// <summary>
        /// Reseta a senha usando token
        /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>
    /// Registra um novo usuário (Admin only)
    /// </summary>
    [HttpPost("register")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var createResult = await _identityService.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken: cancellationToken);

        if (createResult.IsFailure)
        {
            return BadRequest(new { error = createResult.Error.Description });
        }

        var userId = createResult.Value;
        var role = string.IsNullOrEmpty(request.Role) ? "Employee" : request.Role;
        await _identityService.AddToRoleAsync(userId, role, cancellationToken);

        _logger.LogInformation("User registered: {Email} with role {Role}", request.Email, role);

        return Created($"/api/auth/users/{userId}", new RegisterResponse(userId, request.Email));
    }

    /// <summary>
    /// Obtém informações do usuário atual
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _identityService.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return NotFound();
        }

        var claims = await _identityService.GetUserClaimsAsync(userId.Value, cancellationToken);

        return Ok(new UserInfoResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.EmployeeId,
            user.IsActive,
            user.Roles.ToList(),
            claims.ToDictionary(c => c.Key, c => c.Value)));
    }

    /// <summary>
    /// Altera a senha do usuário atual
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var result = await _identityService.ChangePasswordAsync(
            userId.Value,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        return NoContent();
    }

    /// <summary>
    /// Adiciona role a um usuário (Admin only)
    /// </summary>
    [HttpPost("users/{userId:guid}/roles")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddRole(
        Guid userId,
        [FromBody] AddRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.AddToRoleAsync(userId, request.Role, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        _logger.LogInformation("Role {Role} added to user {UserId}", request.Role, userId);
        return NoContent();
    }

    /// <summary>
    /// Remove role de um usuário (Admin only)
    /// </summary>
    [HttpDelete("users/{userId:guid}/roles/{role}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveRole(
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.RemoveFromRoleAsync(userId, role, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        _logger.LogInformation("Role {Role} removed from user {UserId}", role, userId);
        return NoContent();
    }

    /// <summary>
    /// Adiciona claim a um usuário (Admin only)
    /// </summary>
    /// <remarks>
    /// Claims são pares chave-valor que representam permissões ou atributos do usuário.
    /// 
    /// **Exemplos de claims:**
    /// - department: Financeiro
    /// - can_approve_expenses: true
    /// - max_approval_limit: 10000
    /// </remarks>
    [HttpPost("users/{userId:guid}/claims")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddClaim(
        Guid userId,
        [FromBody] AddClaimRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.AddClaimAsync(userId, request.Type, request.Value, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        _logger.LogInformation("Claim {Type}={Value} added to user {UserId}", request.Type, request.Value, userId);
        return NoContent();
    }

    /// <summary>
    /// Obtém claims de um usuário (Admin only)
    /// </summary>
    /// <remarks>
    /// Retorna todas as claims customizadas associadas ao usuário.
    /// Claims de sistema (como roles) não são incluídas nesta lista.
    /// </remarks>
    /// <param name="userId">ID do usuário</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Dicionário de claims (tipo → valor)</returns>
    [HttpGet("users/{userId:guid}/claims")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserClaims(Guid userId, CancellationToken cancellationToken)
    {
        var claims = await _identityService.GetUserClaimsAsync(userId, cancellationToken);
        return Ok(claims.ToDictionary(c => c.Key, c => c.Value));
    }

    /// <summary>
    /// Remove claim de um usuário (Admin only)
    /// </summary>
    /// <remarks>
    /// Remove uma claim específica do usuário pelo tipo.
    /// </remarks>
    /// <param name="userId">ID do usuário</param>
    /// <param name="claimType">Tipo da claim a ser removida</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    [HttpDelete("users/{userId:guid}/claims/{claimType}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveClaim(
        Guid userId,
        string claimType,
        CancellationToken cancellationToken)
    {
        var result = await _identityService.RemoveClaimAsync(userId, claimType, cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Description });
        }

        _logger.LogInformation("Claim {Type} removed from user {UserId}", claimType, userId);
        return NoContent();
    }

    /// <summary>
    /// Lista todas as roles disponíveis (sistema + customizadas)
    /// </summary>
    [HttpGet("roles")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableRoles(
        [FromServices] IRoleService roleService,
        CancellationToken cancellationToken)
    {
        var roles = await roleService.GetAllRolesAsync(cancellationToken);
        return Ok(roles.Select(r => r.Name));
    }

    #region Private Methods

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private string? GetIpAddress()
    {
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            return forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    #endregion
}

#region DTOs

public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record RevokeTokenRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserResponse User);
public record UserResponse(Guid Id, string Email, string FullName, List<string> Roles);
public record RegisterRequest(string Email, string Password, string FirstName, string LastName, string? Role = null);
public record RegisterResponse(Guid UserId, string Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AddRoleRequest(string Role);
public record AddClaimRequest(string Type, string Value);
public record UserInfoResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    Guid? EmployeeId,
    bool IsActive,
    List<string> Roles,
    Dictionary<string, string> Claims);

#endregion