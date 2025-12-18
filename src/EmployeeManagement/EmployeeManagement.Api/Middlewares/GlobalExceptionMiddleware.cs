using EmployeeManagement.Api.Contracts;
using EmployeeManagement.Api.Infrastructure;
using EmployeeManagement.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace EmployeeManagement.Api.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var errorResponse = CreateErrorResponse(exception, traceId);

        // Log estruturado com informações do contrato
        LogError(exception, errorResponse, context);

        context.Response.StatusCode = errorResponse.Status;
        context.Response.ContentType = "application/problem+json";
        
        // Header com versão do contrato de erro
        context.Response.Headers["X-Error-Contract-Version"] = errorResponse.ContractVersion;
        context.Response.Headers["X-Trace-Id"] = traceId;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }));
    }

    private ApiErrorResponse CreateErrorResponse(Exception exception, string traceId)
    {
        return exception switch
        {
            ValidationException validationEx => CreateValidationErrorResponse(validationEx, traceId),
            UnauthorizedAccessException => ErrorResponseFactory.Unauthorized(
                "You are not authorized to access this resource.", 
                "UNAUTHORIZED_ACCESS", 
                traceId),
            KeyNotFoundException ex => ErrorResponseFactory.FromDomainError(
                Error.NotFound("Resource", ex.Message),
                404,
                traceId),
            OperationCanceledException => new ApiErrorResponse
            {
                TraceId = traceId,
                Type = "https://api.employeemanagement.com/errors/cancelled",
                Title = "Request Cancelled",
                Status = 499,
                Detail = "The request was cancelled by the client.",
                ErrorCategory = ErrorCategory.FrontendMisuse,
                ErrorCode = "REQUEST_CANCELLED",
                FrontendAction = FrontendAction.Ignore,
                Retryable = true,
                Timestamp = DateTime.UtcNow.ToString("O")
            },
            _ => ErrorResponseFactory.InternalError(
                traceId, 
                _environment.IsDevelopment(), 
                exception.Message)
        };
    }

    private static ApiErrorResponse CreateValidationErrorResponse(ValidationException ex, string traceId)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return ErrorResponseFactory.FromValidationErrors(errors, traceId);
    }

    private void LogError(Exception exception, ApiErrorResponse errorResponse, HttpContext context)
    {
        var severity = GetLogSeverity(errorResponse.Status);
        
        // Log estruturado com todas as informações do contrato
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = errorResponse.TraceId,
            ["ErrorCategory"] = errorResponse.ErrorCategory,
            ["ErrorCode"] = errorResponse.ErrorCode,
            ["FrontendAction"] = errorResponse.FrontendAction,
            ["Severity"] = severity,
            ["StatusCode"] = errorResponse.Status,
            ["RequestPath"] = context.Request.Path.Value ?? "",
            ["RequestMethod"] = context.Request.Method,
            ["UserIdentity"] = context.User.Identity?.Name ?? "anonymous"
        }))
        {
            if (errorResponse.Status >= 500)
            {
                _logger.LogError(exception,
                    "Error occurred: {ErrorCode} - {Detail} | Action: {FrontendAction} | Severity: {Severity}",
                    errorResponse.ErrorCode,
                    errorResponse.Detail,
                    errorResponse.FrontendAction,
                    severity);
            }
            else
            {
                _logger.LogWarning(
                    "Client error: {ErrorCode} - {Detail} | Action: {FrontendAction}",
                    errorResponse.ErrorCode,
                    errorResponse.Detail,
                    errorResponse.FrontendAction);
            }
        }
    }

    private static string GetLogSeverity(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => ErrorSeverity.CriticalFlow,
            403 or 401 => ErrorSeverity.UserBlocking,
            _ => ErrorSeverity.LowImpact
        };
    }
}