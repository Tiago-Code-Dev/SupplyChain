using System.Text.Json.Serialization;

namespace EmployeeManagement.Api.Contracts;

/// <summary>
/// Resposta de erro padronizada (RFC 7807 / RFC 9457 Extended)
/// Contrato oficial entre Backend e Frontend
/// </summary>
public sealed record ApiErrorResponse
{
    /// <summary>
    /// ID único para rastreamento (correlação com logs)
    /// </summary>
    [JsonPropertyName("traceId")]
    public required string TraceId { get; init; }

    /// <summary>
    /// URI de referência do tipo de erro
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Título resumido do erro
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// HTTP Status Code
    /// </summary>
    [JsonPropertyName("status")]
    public required int Status { get; init; }

    /// <summary>
    /// Descrição detalhada do erro (amigável ao usuário)
    /// </summary>
    [JsonPropertyName("detail")]
    public required string Detail { get; init; }

    /// <summary>
    /// Categoria do erro (VALIDATION_ERROR, AUTH_ERROR, etc.)
    /// </summary>
    [JsonPropertyName("errorCategory")]
    public required string ErrorCategory { get; init; }

    /// <summary>
    /// Código único e estável do erro (para mapeamento no frontend)
    /// </summary>
    [JsonPropertyName("errorCode")]
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Lista de erros de validação por campo
    /// </summary>
    [JsonPropertyName("errors")]
    public List<FieldError>? Errors { get; init; }

    /// <summary>
    /// Ação que o frontend deve executar
    /// </summary>
    [JsonPropertyName("frontendAction")]
    public required string FrontendAction { get; init; }

    /// <summary>
    /// Indica se a operação pode ser retentada
    /// </summary>
    [JsonPropertyName("retryable")]
    public bool Retryable { get; init; }

    /// <summary>
    /// Timestamp do erro (ISO 8601)
    /// </summary>
    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    /// <summary>
    /// Versão do contrato de erro
    /// </summary>
    [JsonPropertyName("contractVersion")]
    public string ContractVersion { get; init; } = "1.0";
}

/// <summary>
/// Erro de validação por campo
/// </summary>
public sealed record FieldError
{
    [JsonPropertyName("field")]
    public required string Field { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }

    [JsonPropertyName("code")]
    public string? Code { get; init; }
}