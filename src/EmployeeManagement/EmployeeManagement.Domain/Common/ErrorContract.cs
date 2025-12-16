namespace EmployeeManagement.Domain.Common;

/// <summary>
/// Categorias de erro padronizadas para o frontend
/// </summary>
public static class ErrorCategory
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string AuthError = "AUTH_ERROR";
    public const string AuthorizationError = "AUTHORIZATION_ERROR";
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string IntegrationFailure = "INTEGRATION_FAILURE";
    public const string RateLimit = "RATE_LIMIT";
    public const string FrontendMisuse = "FRONTEND_MISUSE";
    public const string InternalError = "INTERNAL_ERROR";
}

/// <summary>
/// Ações que o frontend deve executar
/// </summary>
public static class FrontendAction
{
    public const string ShowToast = "SHOW_TOAST";
    public const string ShowModal = "SHOW_MODAL";
    public const string HighlightField = "HIGHLIGHT_FIELD";
    public const string ForceLogout = "FORCE_LOGOUT";
    public const string RedirectLogin = "REDIRECT_LOGIN";
    public const string Retry = "RETRY";
    public const string BlockFlow = "BLOCK_FLOW";
    public const string Ignore = "IGNORE";
}

/// <summary>
/// Níveis de severidade do erro
/// </summary>
public static class ErrorSeverity
{
    public const string LowImpact = "LOW_IMPACT";
    public const string UserBlocking = "USER_BLOCKING";
    public const string CriticalFlow = "CRITICAL_FLOW";
}