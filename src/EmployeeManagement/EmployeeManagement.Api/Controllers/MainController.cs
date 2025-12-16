using EmployeeManagement.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
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
        
        if (statusCode == StatusCodes.Status403Forbidden)
        {
            return Forbid();
        }

        return StatusCode(statusCode, CreateProblemDetails(error, statusCode));
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

    private static ProblemDetails CreateProblemDetails(Error error, int statusCode) => new()
    {
        Status = statusCode,
        Title = GetTitleFromCode(error.Code),
        Detail = error.Description,
        Type = $"https://httpstatuses.com/{statusCode}",
        Extensions =
        {
            ["errorCode"] = error.Code,
            ["traceId"] = Guid.NewGuid().ToString()
        }
    };

    private static string GetTitleFromCode(string code)
    {
        return code switch
        {
            _ when code.Contains("NotFound") => "Resource Not Found",
            _ when code.Contains("Conflict") => "Resource Conflict",
            _ when code.Contains("Forbidden") => "Access Forbidden",
            _ when code.Contains("Unauthorized") => "Unauthorized Access",
            _ when code.Contains("Validation") => "Validation Error",
            _ => "Bad Request"
        };
    }

    #endregion
}