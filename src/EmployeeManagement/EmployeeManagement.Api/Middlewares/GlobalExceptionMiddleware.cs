using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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
        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            UnauthorizedAccessException => HandleUnauthorizedException(),
            KeyNotFoundException => HandleNotFoundException(),
            _ => HandleUnknownException(exception)
        };

        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static (int StatusCode, ProblemDetails Problem) HandleValidationException(ValidationException ex)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return (StatusCodes.Status400BadRequest, new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Error",
            Detail = "One or more validation errors occurred.",
            Type = "https://httpstatuses.com/400"
        });
    }

    private static (int StatusCode, ProblemDetails Problem) HandleUnauthorizedException()
    {
        return (StatusCodes.Status401Unauthorized, new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = "Authentication is required.",
            Type = "https://httpstatuses.com/401"
        });
    }

    private static (int StatusCode, ProblemDetails Problem) HandleNotFoundException()
    {
        return (StatusCodes.Status404NotFound, new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not Found",
            Detail = "The requested resource was not found.",
            Type = "https://httpstatuses.com/404"
        });
    }

    private (int StatusCode, ProblemDetails Problem) HandleUnknownException(Exception ex)
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = _environment.IsDevelopment() 
                ? ex.Message 
                : "An unexpected error occurred. Please try again later.",
            Type = "https://httpstatuses.com/500"
        };

        return (StatusCodes.Status500InternalServerError, problemDetails);
    }
}