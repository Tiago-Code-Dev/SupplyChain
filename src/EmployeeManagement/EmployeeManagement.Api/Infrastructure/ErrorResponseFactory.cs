using EmployeeManagement.Api.Contracts;
using EmployeeManagement.Domain.Common;
using System.Diagnostics;

namespace EmployeeManagement.Api.Infrastructure;

/// <summary>
/// Factory para criar respostas de erro padronizadas
/// </summary>
public static class ErrorResponseFactory
{
    private const string ErrorContractVersion = "1.0";

    /// <summary>
    /// Cria resposta de erro a partir de um Error do domínio
    /// </summary>
    public static ApiErrorResponse FromDomainError(Error error, int statusCode, string? traceId = null)
    {
        var (category, frontendAction, retryable) = MapErrorToCategory(error, statusCode);
        
        return new ApiErrorResponse
        {
            TraceId = traceId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Type = $"https://api.employeemanagement.com/errors/{error.Code.ToLowerInvariant().Replace(".", "/")}",
            Title = GetTitleFromCode(error.Code),
            Status = statusCode,
            Detail = error.Description,
            ErrorCategory = category,
            ErrorCode = error.Code,
            FrontendAction = frontendAction,
            Retryable = retryable,
            Timestamp = DateTime.UtcNow.ToString("O"),
            ContractVersion = ErrorContractVersion
        };
    }

    /// <summary>
    /// Cria resposta de erro de validação com múltiplos campos
    /// </summary>
    public static ApiErrorResponse FromValidationErrors(
        IDictionary<string, string[]> errors, 
        string? traceId = null)
    {
        var fieldErrors = errors
            .SelectMany(kvp => kvp.Value.Select(msg => new FieldError
            {
                Field = kvp.Key,
                Message = msg,
                Code = $"{kvp.Key}.Validation"
            }))
            .ToList();

        return new ApiErrorResponse
        {
            TraceId = traceId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Type = "https://api.employeemanagement.com/errors/validation",
            Title = "Validation Error",
            Status = 400,
            Detail = "One or more validation errors occurred.",
            ErrorCategory = ErrorCategory.ValidationError,
            ErrorCode = "VALIDATION_FAILED",
            Errors = fieldErrors,
            FrontendAction = FrontendAction.HighlightField,
            Retryable = false,
            Timestamp = DateTime.UtcNow.ToString("O"),
            ContractVersion = ErrorContractVersion
        };
    }

    /// <summary>
    /// Cria resposta de erro interno (500)
    /// </summary>
    public static ApiErrorResponse InternalError(string? traceId = null, bool isDevelopment = false, string? exceptionMessage = null)
    {
        return new ApiErrorResponse
        {
            TraceId = traceId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Type = "https://api.employeemanagement.com/errors/internal",
            Title = "Internal Server Error",
            Status = 500,
            Detail = isDevelopment && exceptionMessage != null 
                ? exceptionMessage 
                : "An unexpected error occurred. Please try again later.",
            ErrorCategory = ErrorCategory.InternalError,
            ErrorCode = "INTERNAL_ERROR",
            FrontendAction = FrontendAction.ShowModal,
            Retryable = true,
            Timestamp = DateTime.UtcNow.ToString("O"),
            ContractVersion = ErrorContractVersion
        };
    }

    /// <summary>
    /// Cria resposta de erro de autenticação (401)
    /// </summary>
    public static ApiErrorResponse Unauthorized(string message, string errorCode = "AUTH_FAILED", string? traceId = null)
    {
        return new ApiErrorResponse
        {
            TraceId = traceId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Type = "https://api.employeemanagement.com/errors/auth/unauthorized",
            Title = "Unauthorized",
            Status = 401,
            Detail = message,
            ErrorCategory = ErrorCategory.AuthError,
            ErrorCode = errorCode,
            FrontendAction = FrontendAction.RedirectLogin,
            Retryable = false,
            Timestamp = DateTime.UtcNow.ToString("O"),
            ContractVersion = ErrorContractVersion
        };
    }

    /// <summary>
    /// Cria resposta de erro de autorização (403)
    /// </summary>
    public static ApiErrorResponse Forbidden(string message, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            TraceId = traceId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Type = "https://api.employeemanagement.com/errors/auth/forbidden",
            Title = "Forbidden",
            Status = 403,
            Detail = message,
            ErrorCategory = ErrorCategory.AuthorizationError,
            ErrorCode = "ACCESS_DENIED",
            FrontendAction = FrontendAction.ShowModal,
            Retryable = false,
            Timestamp = DateTime.UtcNow.ToString("O"),
            ContractVersion = ErrorContractVersion
        };
    }

    /// <summary>
    /// Cria resposta de rate limit (429)
    /// </summary>
    public static ApiErrorResponse RateLimitExceeded(string? traceId = null, int? retryAfterSeconds = null)
    {
        return new ApiErrorResponse
        {
            TraceId = traceId ?? Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            Type = "https://api.employeemanagement.com/errors/rate-limit",
            Title = "Too Many Requests",
            Status = 429,
            Detail = retryAfterSeconds.HasValue 
                ? $"Rate limit exceeded. Please try again in {retryAfterSeconds} seconds."
                : "Rate limit exceeded. Please try again later.",
            ErrorCategory = ErrorCategory.RateLimit,
            ErrorCode = "RATE_LIMIT_EXCEEDED",
            FrontendAction = FrontendAction.Retry,
            Retryable = true,
            Timestamp = DateTime.UtcNow.ToString("O"),
            ContractVersion = ErrorContractVersion
        };
    }

    /// <summary>
    /// Mapeia Error do domínio para categoria, ação e retryable
    /// </summary>
    private static (string Category, string Action, bool Retryable) MapErrorToCategory(Error error, int statusCode)
    {
        return statusCode switch
        {
            400 when error.Code.Contains("Validation") => 
                (ErrorCategory.ValidationError, FrontendAction.HighlightField, false),
            400 => 
                (ErrorCategory.BusinessRuleViolation, FrontendAction.ShowToast, false),
            401 => 
                (ErrorCategory.AuthError, FrontendAction.RedirectLogin, false),
            403 => 
                (ErrorCategory.AuthorizationError, FrontendAction.ShowModal, false),
            404 => 
                (ErrorCategory.ResourceNotFound, FrontendAction.ShowToast, false),
            409 => 
                (ErrorCategory.Conflict, FrontendAction.ShowModal, false),
            429 => 
                (ErrorCategory.RateLimit, FrontendAction.Retry, true),
            >= 500 => 
                (ErrorCategory.InternalError, FrontendAction.ShowModal, true),
            _ => 
                (ErrorCategory.InternalError, FrontendAction.ShowToast, false)
        };
    }

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
}