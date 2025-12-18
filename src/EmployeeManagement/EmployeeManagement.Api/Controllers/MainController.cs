using Asp.Versioning;
using EmployeeManagement.Api.Contracts;
using EmployeeManagement.Api.Infrastructure;
using EmployeeManagement.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// Controller base com funcionalidades comuns
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class MainController : ControllerBase
{
    protected readonly ISender Sender;

    protected MainController(ISender sender)
    {
        Sender = sender;
    }

    #region User Claims

    protected Guid? CurrentUserId
    {
        get
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    protected string? CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

    protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;

    protected TRole GetCurrentUserRole<TRole>() where TRole : struct, Enum
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<TRole>(roleClaim, out var role) ? role : default;
    }

    protected bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    #endregion

    #region Tracing

    protected string GetTraceId() => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    #endregion

    #region Result Handling

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        return result.Match(
            value => Ok(value),
            error => HandleError(error));
    }

    protected IActionResult HandleResult(Result result)
    {
        return result.IsSuccess
            ? NoContent()
            : HandleError(result.Error);
    }

    protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName, Func<T, object> routeValues)
    {
        return result.Match(
            value => CreatedAtAction(actionName, routeValues(value), value),
            error => HandleError(error));
    }

    protected IActionResult HandleError(Error error)
    {
        var statusCode = GetStatusCodeFromError(error);
        var traceId = GetTraceId();
        
        Response.Headers["X-Trace-Id"] = traceId;
        Response.Headers["X-Error-Contract-Version"] = "1.0";

        if (statusCode == StatusCodes.Status403Forbidden)
        {
            var forbiddenResponse = ErrorResponseFactory.Forbidden(error.Description, traceId);
            return StatusCode(403, forbiddenResponse);
        }

        var errorResponse = ErrorResponseFactory.FromDomainError(error, statusCode, traceId);
        return StatusCode(statusCode, errorResponse);
    }

    private static int GetStatusCodeFromError(Error error)
    {
        return error.Code switch
        {
            _ when error.Code.EndsWith(".NotFound") => StatusCodes.Status404NotFound,
            _ when error.Code.EndsWith(".Conflict") => StatusCodes.Status409Conflict,
            _ when error.Code.Contains("Forbidden") => StatusCodes.Status403Forbidden,
            _ when error.Code.Contains("Unauthorized") => StatusCodes.Status401Unauthorized,
            _ when error.Code.Contains("Validation") => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }

    #endregion
}