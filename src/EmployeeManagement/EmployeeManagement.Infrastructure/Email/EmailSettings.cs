namespace EmployeeManagement.Infrastructure.Email;

/// <summary>
/// Configurações para o serviço de email SMTP
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>
    /// Servidor SMTP (ex: smtp.gmail.com, smtp.office365.com)
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// Porta SMTP (587 para TLS, 465 para SSL, 25 para sem criptografia)
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Email do remetente
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// Nome do remetente que aparecerá no email
    /// </summary>
    public string SenderName { get; set; } = "Employee Management System";

    /// <summary>
    /// Usuário para autenticação SMTP (geralmente o mesmo que SenderEmail)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Senha ou App Password para autenticação SMTP
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Usar SSL/TLS
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// URL base do frontend para links nos emails
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    /// <summary>
    /// Habilitar envio de emails (false para desenvolvimento/testes)
    /// </summary>
    public bool EnableSending { get; set; } = true;

    /// <summary>
    /// Email para receber cópias de todos os emails enviados (BCC)
    /// </summary>
    public string? BccEmail { get; set; }
}
