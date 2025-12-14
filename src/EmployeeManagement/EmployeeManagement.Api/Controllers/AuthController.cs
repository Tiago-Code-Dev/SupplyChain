using Asp.Versioning;
using EmployeeManagement.Api.Contracts;
using EmployeeManagement.Application.Features.Auth.Commands.ChangePassword;
using EmployeeManagement.Application.Features.Auth.Commands.Login;
using EmployeeManagement.Application.Features.Auth.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// Controller para autenticação
/// </summary>
[Tags("Auth")]
public class AuthController : MainController
{
    public AuthController(ISender sender) : base(sender) { }

    /// <summary>
    /// Realiza login no sistema
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Altera a senha do usuário autenticado
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
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var command = new ChangePasswordCommand(
            CurrentUserId.Value,
            request.CurrentPassword,
            request.NewPassword);

        var result = await Sender.Send(command, cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Retorna informações do usuário autenticado
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        if (!IsAuthenticated || CurrentUserId is null)
        {
            return Unauthorized();
        }

        return Ok(new UserInfo(
            CurrentUserId.Value,
            CurrentUserEmail,
            CurrentUserRole));
    }
}

public record UserInfo(Guid Id, string? Email, string? Role);