using System.Security.Claims;
using Asp.Versioning;
using EmployeeManagement.Api.Configurations;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmployeeManagement.Api.V2.Controllers;

/// <summary>
/// Controller para autenticação - V2
/// </summary>
/// <remarks>
/// V2 mantém compatibilidade com V1, preparado para futuras evoluções.
/// </remarks>
[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("2.0")]
[Tags("Auth")]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IIdentityService identityService, ILogger<AuthController> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza login no sistema (V2)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(AuthResponseV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequestV2 request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        _logger.LogInformation("V2 Login attempt from IP: {IpAddress} for email: {Email}", ipAddress, request.Email);
        
        var result = await _identityService.AuthenticateAsync(request.Email, request.Password, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("V2 Login failed from IP: {IpAddress} for email: {Email}", ipAddress, request.Email);
            return Unauthorized(new ErrorResponseV2("AUTH_FAILED", result.Error.Description));
        }

        var authResult = result.Value;
        return Ok(new AuthResponseV2
        {
            AccessToken = authResult.AccessToken,
            RefreshToken = authResult.RefreshToken,
            ExpiresAt = authResult.AccessTokenExpiresAt,
            TokenType = "Bearer",
            User = new UserResponseV2
            {
                Id = authResult.UserId,
                Email = authResult.Email,
                FullName = authResult.FullName,
                Roles = authResult.Roles.ToList()
            }
        });
    }

    /// <summary>
    /// Renova o access token usando refresh token (V2)
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingConfiguration.LoginPolicy)]
    [ProducesResponseType(typeof(AuthResponseV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestV2 request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _identityService.RefreshTokenAsync(request.RefreshToken, ipAddress, cancellationToken);

        if (result.IsFailure)
        {
            return Unauthorized(new ErrorResponseV2("TOKEN_INVALID", result.Error.Description));
        }

        var authResult = result.Value;
        return Ok(new AuthResponseV2
        {
            AccessToken = authResult.AccessToken,
            RefreshToken = authResult.RefreshToken,
            ExpiresAt = authResult.AccessTokenExpiresAt,
            TokenType = "Bearer",
            User = new UserResponseV2
            {
                Id = authResult.UserId,
                Email = authResult.Email,
                FullName = authResult.FullName,
                Roles = authResult.Roles.ToList()
            }
        });
    }

    /// <summary>
    /// Logout - revoga o refresh token (V2)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestV2 request, CancellationToken cancellationToken)
    {
        var ipAddress = GetIpAddress();
        var result = await _identityService.RevokeTokenAsync(request.RefreshToken, ipAddress, "User logout V2", cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(new ErrorResponseV2("LOGOUT_FAILED", result.Error.Description));
        }

        return NoContent();
    }

    /// <summary>
    /// Obtém informações do usuário atual (V2)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileV2), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _identityService.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            return NotFound(new ErrorResponseV2("USER_NOT_FOUND", "Usuário não encontrado"));
        }

        var claims = await _identityService.GetUserClaimsAsync(userId.Value, cancellationToken);

        return Ok(new UserProfileV2
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            EmployeeId = user.EmployeeId,
            IsActive = user.IsActive,
            Roles = user.Roles.ToList(),
            Permissions = claims.ToDictionary(c => c.Key, c => c.Value)
        });
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

#region V2 DTOs

/// <summary>
/// Request de login V2
/// </summary>
public record LoginRequestV2(string Email, string Password);

/// <summary>
/// Request de refresh token V2
/// </summary>
public record RefreshRequestV2(string RefreshToken);

/// <summary>
/// Request de logout V2
/// </summary>
public record LogoutRequestV2(string RefreshToken);

/// <summary>
/// Response de autenticação V2 - estrutura simplificada
/// </summary>
public record AuthResponseV2
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required string TokenType { get; init; }
    public required UserResponseV2 User { get; init; }
}

/// <summary>
/// Response de usuário V2
/// </summary>
public record UserResponseV2
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required List<string> Roles { get; init; }
}

/// <summary>
/// Perfil completo do usuário V2
/// </summary>
public record UserProfileV2
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public Guid? EmployeeId { get; init; }
    public required bool IsActive { get; init; }
    public required List<string> Roles { get; init; }
    public required Dictionary<string, string> Permissions { get; init; }
}

/// <summary>
/// Response de erro V2 - estrutura simplificada
/// </summary>
public record ErrorResponseV2(string Code, string Message);

#endregion