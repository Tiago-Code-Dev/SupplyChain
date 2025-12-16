using Asp.Versioning;
using EmployeeManagement.Api.Contracts;
using EmployeeManagement.Api.Infrastructure;
using EmployeeManagement.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// Documentação do Error Contract para desenvolvedores frontend
/// </summary>
[Route("api/v{version:apiVersion}/errorcontract")]
[Route("api/errorcontract")]
[ApiController]
[ApiVersion("1.0")]
[Tags("Error Contract")]
[AllowAnonymous]
public class ErrorContractController : ControllerBase
{
    /// <summary>
    /// Retorna a documentação completa do Error Contract
    /// </summary>
    /// <remarks>
    /// Este endpoint fornece toda a documentação necessária para o frontend
    /// implementar o tratamento de erros de forma consistente.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ErrorContractDocumentation), StatusCodes.Status200OK)]
    public IActionResult GetContract()
    {
        var documentation = new ErrorContractDocumentation
        {
            Version = "1.0",
            Description = "Error & Log Contract oficial entre Backend e Frontend",
            LastUpdated = "2024-12-15",
            Categories = new Dictionary<string, CategoryInfo>
            {
                [ErrorCategory.ValidationError] = new()
                {
                    Description = "Erro de validação de dados de entrada",
                    HttpStatus = 400,
                    DefaultAction = FrontendAction.HighlightField,
                    Retryable = false,
                    UserMessage = "Por favor, corrija os campos destacados"
                },
                [ErrorCategory.AuthError] = new()
                {
                    Description = "Erro de autenticação (credenciais inválidas ou token expirado)",
                    HttpStatus = 401,
                    DefaultAction = FrontendAction.RedirectLogin,
                    Retryable = false,
                    UserMessage = "Sua sessão expirou. Por favor, faça login novamente"
                },
                [ErrorCategory.AuthorizationError] = new()
                {
                    Description = "Erro de autorização (sem permissão para a ação)",
                    HttpStatus = 403,
                    DefaultAction = FrontendAction.ShowModal,
                    Retryable = false,
                    UserMessage = "Você não tem permissão para realizar esta ação"
                },
                [ErrorCategory.BusinessRuleViolation] = new()
                {
                    Description = "Violação de regra de negócio",
                    HttpStatus = 400,
                    DefaultAction = FrontendAction.ShowToast,
                    Retryable = false,
                    UserMessage = "A operação não pode ser realizada devido a uma regra de negócio"
                },
                [ErrorCategory.ResourceNotFound] = new()
                {
                    Description = "Recurso não encontrado",
                    HttpStatus = 404,
                    DefaultAction = FrontendAction.ShowToast,
                    Retryable = false,
                    UserMessage = "O recurso solicitado não foi encontrado"
                },
                [ErrorCategory.Conflict] = new()
                {
                    Description = "Conflito de dados (ex: email já cadastrado)",
                    HttpStatus = 409,
                    DefaultAction = FrontendAction.ShowModal,
                    Retryable = false,
                    UserMessage = "Conflito detectado. O recurso já existe ou foi modificado"
                },
                [ErrorCategory.RateLimit] = new()
                {
                    Description = "Limite de requisições excedido",
                    HttpStatus = 429,
                    DefaultAction = FrontendAction.Retry,
                    Retryable = true,
                    UserMessage = "Muitas tentativas. Por favor, aguarde alguns segundos"
                },
                [ErrorCategory.InternalError] = new()
                {
                    Description = "Erro interno do servidor",
                    HttpStatus = 500,
                    DefaultAction = FrontendAction.ShowModal,
                    Retryable = true,
                    UserMessage = "Ocorreu um erro inesperado. Tente novamente mais tarde"
                }
            },
            Actions = new Dictionary<string, ActionInfo>
            {
                [FrontendAction.ShowToast] = new()
                {
                    Description = "Exibir notificação toast temporária",
                    Duration = "3-5 segundos",
                    Blocking = false
                },
                [FrontendAction.ShowModal] = new()
                {
                    Description = "Exibir modal de erro que requer interação do usuário",
                    Duration = "Até usuário fechar",
                    Blocking = true
                },
                [FrontendAction.HighlightField] = new()
                {
                    Description = "Destacar campos com erro de validação",
                    Duration = "Até correção",
                    Blocking = false
                },
                [FrontendAction.ForceLogout] = new()
                {
                    Description = "Forçar logout e limpar tokens",
                    Duration = "Imediato",
                    Blocking = true
                },
                [FrontendAction.RedirectLogin] = new()
                {
                    Description = "Redirecionar para tela de login",
                    Duration = "Imediato",
                    Blocking = true
                },
                [FrontendAction.Retry] = new()
                {
                    Description = "Exibir opção de retry com backoff",
                    Duration = "Variável",
                    Blocking = false
                },
                [FrontendAction.BlockFlow] = new()
                {
                    Description = "Bloquear fluxo atual até resolução",
                    Duration = "Até resolução",
                    Blocking = true
                },
                [FrontendAction.Ignore] = new()
                {
                    Description = "Ignorar erro silenciosamente (apenas log)",
                    Duration = "Nenhum",
                    Blocking = false
                }
            },
            CommonErrorCodes = new Dictionary<string, ErrorCodeInfo>
            {
                ["VALIDATION_FAILED"] = new()
                {
                    Description = "Um ou mais campos falharam na validação",
                    Category = ErrorCategory.ValidationError,
                    Action = FrontendAction.HighlightField
                },
                ["AUTH_FAILED"] = new()
                {
                    Description = "Credenciais inválidas",
                    Category = ErrorCategory.AuthError,
                    Action = FrontendAction.ShowToast
                },
                ["TOKEN_EXPIRED"] = new()
                {
                    Description = "Token JWT expirado",
                    Category = ErrorCategory.AuthError,
                    Action = FrontendAction.RedirectLogin
                },
                ["ACCESS_DENIED"] = new()
                {
                    Description = "Sem permissão para a ação",
                    Category = ErrorCategory.AuthorizationError,
                    Action = FrontendAction.ShowModal
                },
                ["RESOURCE_NOT_FOUND"] = new()
                {
                    Description = "Recurso não encontrado",
                    Category = ErrorCategory.ResourceNotFound,
                    Action = FrontendAction.ShowToast
                },
                ["EMAIL_ALREADY_EXISTS"] = new()
                {
                    Description = "Email já cadastrado",
                    Category = ErrorCategory.Conflict,
                    Action = FrontendAction.HighlightField
                },
                ["DOCUMENT_ALREADY_EXISTS"] = new()
                {
                    Description = "Documento já cadastrado",
                    Category = ErrorCategory.Conflict,
                    Action = FrontendAction.HighlightField
                },
                ["CANNOT_CREATE_HIGHER_ROLE"] = new()
                {
                    Description = "Não pode criar usuário com role superior",
                    Category = ErrorCategory.AuthorizationError,
                    Action = FrontendAction.ShowModal
                },
                ["RATE_LIMIT_EXCEEDED"] = new()
                {
                    Description = "Limite de requisições excedido",
                    Category = ErrorCategory.RateLimit,
                    Action = FrontendAction.Retry
                },
                ["INTERNAL_ERROR"] = new()
                {
                    Description = "Erro interno do servidor",
                    Category = ErrorCategory.InternalError,
                    Action = FrontendAction.ShowModal
                }
            },
            ExampleResponse = new ApiErrorResponse
            {
                TraceId = "00-abc123def456-789xyz-00",
                Type = "https://api.employeemanagement.com/errors/email/conflict",
                Title = "Resource Conflict",
                Status = 409,
                Detail = "Email já cadastrado",
                ErrorCategory = ErrorCategory.Conflict,
                ErrorCode = "EMAIL_ALREADY_EXISTS",
                FrontendAction = FrontendAction.HighlightField,
                Retryable = false,
                Timestamp = DateTime.UtcNow.ToString("O"),
                Errors = new List<FieldError>
                {
                    new() { Field = "email", Message = "Este email já está em uso", Code = "Email.Conflict" }
                }
            }
        };

        return Ok(documentation);
    }

    /// <summary>
    /// Simula um erro para teste do frontend
    /// </summary>
    [HttpPost("test/{errorType}")]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult TestError(string errorType)
    {
        var traceId = Guid.NewGuid().ToString();

        return errorType.ToLower() switch
        {
            "validation" => BadRequest(ErrorResponseFactory.FromValidationErrors(
                new Dictionary<string, string[]>
                {
                    ["email"] = ["Email inválido"],
                    ["password"] = ["Senha muito fraca", "Mínimo 8 caracteres"]
                }, traceId)),
            "unauthorized" => Unauthorized(ErrorResponseFactory.Unauthorized(
                "Token inválido ou expirado", "TOKEN_EXPIRED", traceId)),
            "forbidden" => StatusCode(403, ErrorResponseFactory.Forbidden(
                "Você não tem permissão para esta ação", traceId)),
            "notfound" => NotFound(ErrorResponseFactory.FromDomainError(
                Error.NotFound("Employee", Guid.NewGuid()), 404, traceId)),
            "conflict" => Conflict(ErrorResponseFactory.FromDomainError(
                Error.Conflict("Email", "Email já cadastrado"), 409, traceId)),
            "ratelimit" => StatusCode(429, ErrorResponseFactory.RateLimitExceeded(traceId, 60)),
            "internal" => StatusCode(500, ErrorResponseFactory.InternalError(traceId, true,
                "Simulated internal error for testing")),
            _ => BadRequest(new { error = "Invalid error type. Use: validation, unauthorized, forbidden, notfound, conflict, ratelimit, internal" })
        };
    }
}

#region Documentation DTOs

public record ErrorContractDocumentation
{
    public required string Version { get; init; }
    public required string Description { get; init; }
    public required string LastUpdated { get; init; }
    public required Dictionary<string, CategoryInfo> Categories { get; init; }
    public required Dictionary<string, ActionInfo> Actions { get; init; }
    public required Dictionary<string, ErrorCodeInfo> CommonErrorCodes { get; init; }
    public required ApiErrorResponse ExampleResponse { get; init; }
}

public record CategoryInfo
{
    public required string Description { get; init; }
    public required int HttpStatus { get; init; }
    public required string DefaultAction { get; init; }
    public required bool Retryable { get; init; }
    public required string UserMessage { get; init; }
}

public record ActionInfo
{
    public required string Description { get; init; }
    public required string Duration { get; init; }
    public required bool Blocking { get; init; }
}

public record ErrorCodeInfo
{
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Action { get; init; }
}

#endregion