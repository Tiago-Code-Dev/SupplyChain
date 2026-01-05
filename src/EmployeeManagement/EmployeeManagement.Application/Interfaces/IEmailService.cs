namespace EmployeeManagement.Application.Interfaces;

/// <summary>
/// Interface para serviço de envio de emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia um email
    /// </summary>
    /// <param name="to">Endereço de email do destinatário</param>
    /// <param name="subject">Assunto do email</param>
    /// <param name="body">Corpo do email (HTML)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi enviado com sucesso</returns>
    Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de reset de senha
    /// </summary>
    /// <param name="to">Email do destinatário</param>
    /// <param name="userName">Nome do usuário</param>
    /// <param name="resetToken">Token de reset</param>
    /// <param name="resetUrl">URL base para reset (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi enviado com sucesso</returns>
    Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetToken, string? resetUrl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia email de boas-vindas para novo funcionário
    /// </summary>
    /// <param name="to">Email do destinatário</param>
    /// <param name="userName">Nome do usuário</param>
    /// <param name="temporaryPassword">Senha temporária (opcional)</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>True se o email foi enviado com sucesso</returns>
    Task<bool> SendWelcomeEmailAsync(string to, string userName, string? temporaryPassword = null, CancellationToken cancellationToken = default);
}
