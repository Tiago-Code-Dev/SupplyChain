namespace EmployeeManagement.Infrastructure.Identity;

/// <summary>
/// Configurações tipadas para JWT/Token
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Chave secreta para assinatura do token (mínimo 32 caracteres)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Emissor do token
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Audiência do token
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Tempo de expiração do Access Token em minutos (padrão: 15)
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Tempo de expiração do Refresh Token em dias (padrão: 7)
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
