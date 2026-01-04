using System.Text;
using EmployeeManagement.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace EmployeeManagement.Infrastructure.Email;

/// <summary>
/// Implementação do serviço de email usando MailKit (mais confiável que System.Net.Mail)
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableSending)
        {
            _logger.LogWarning("?? Email sending is DISABLED. Would have sent to: {To}, Subject: {Subject}", to, subject);
            return true;
        }

        if (string.IsNullOrEmpty(_settings.SmtpServer))
        {
            _logger.LogError("? SMTP Server not configured! Cannot send email.");
            return false;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(Encoding.UTF8, _settings.SenderName, _settings.SenderEmail));
            message.To.Add(new MailboxAddress(Encoding.UTF8, to, to));
            message.Subject = subject;

            // Criar corpo HTML com encoding UTF-8 explícito
            var htmlPart = new TextPart(TextFormat.Html)
            {
                Text = body,
                ContentTransferEncoding = ContentEncoding.Base64
            };
            htmlPart.ContentType.Charset = "utf-8";
            message.Body = htmlPart;

            if (!string.IsNullOrEmpty(_settings.BccEmail))
            {
                message.Bcc.Add(new MailboxAddress(_settings.BccEmail, _settings.BccEmail));
            }

            using var client = new SmtpClient();

            _logger.LogInformation("?? Connecting to SMTP: {Server}:{Port}", _settings.SmtpServer, _settings.SmtpPort);

            // Configurar opções de segurança baseado na porta
            var secureSocketOptions = _settings.SmtpPort switch
            {
                465 => SecureSocketOptions.SslOnConnect,
                587 => SecureSocketOptions.StartTls,
                2525 => SecureSocketOptions.StartTlsWhenAvailable, // Mailtrap
                25 => SecureSocketOptions.None,
                _ => _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None
            };

            await client.ConnectAsync(_settings.SmtpServer, _settings.SmtpPort, secureSocketOptions, cancellationToken);

            _logger.LogInformation("?? Authenticating with user: {Username}", _settings.Username);

            if (!string.IsNullOrEmpty(_settings.Username) && !string.IsNullOrEmpty(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            _logger.LogInformation("?? Sending email to: {To}", to);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("? Email sent successfully to {To} with subject: {Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Failed to send email to {To}. Error: {Message}", to, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetToken, string? resetUrl = null, CancellationToken cancellationToken = default)
    {
        var baseUrl = resetUrl ?? _settings.FrontendBaseUrl;
        var resetLink = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(to)}&token={Uri.EscapeDataString(resetToken)}";

        var subject = "Redefinicao de Senha - Employee Management";
        var body = GeneratePasswordResetEmailBody(userName, resetLink, resetToken);

        _logger.LogInformation("?? Sending password reset email to {Email}, ResetLink: {Link}", to, resetLink);

        return await SendEmailAsync(to, subject, body, cancellationToken);
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string userName, string? temporaryPassword = null, CancellationToken cancellationToken = default)
    {
        var subject = "Bem-vindo ao Employee Management System";
        var body = GenerateWelcomeEmailBody(userName, to, temporaryPassword);

        return await SendEmailAsync(to, subject, body, cancellationToken);
    }

        private static string GeneratePasswordResetEmailBody(string userName, string resetLink, string token)
        {
            return $@"<!DOCTYPE html>
    <html lang=""pt-BR"">
    <head>
        <meta charset=""UTF-8"">
        <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Redefinicao de Senha</title>
    </head>
    <body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333333; background-color: #f5f5f5;"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; margin: 0 auto; background-color: #f5f5f5;"">
            <tr>
                <td style=""padding: 20px;"">
                    <!-- Header -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                            <td style=""background-color: #667eea; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
                                <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">Redefinicao de Senha</h1>
                            </td>
                        </tr>
                    </table>

                    <!-- Body -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                            <td style=""background-color: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;"">
                                <p style=""font-size: 16px; margin: 0 0 15px 0;"">Ola <strong>{userName}</strong>,</p>

                                <p style=""font-size: 16px; margin: 0 0 25px 0;"">Recebemos uma solicitacao para redefinir a senha da sua conta no <strong>Employee Management System</strong>.</p>

                                <!-- Button -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                    <tr>
                                        <td align=""center"" style=""padding: 25px 0;"">
                                            <table cellpadding=""0"" cellspacing=""0"">
                                                <tr>
                                                    <td style=""background-color: #667eea; border-radius: 5px;"">
                                                        <a href=""{resetLink}"" target=""_blank"" style=""display: inline-block; padding: 15px 30px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold;"">Redefinir Minha Senha</a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>

                                <!-- Token Box -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0;"">
                                    <tr>
                                        <td style=""background-color: #f8f9fa; padding: 15px; border-left: 4px solid #667eea;"">
                                            <p style=""margin: 0; font-size: 14px; color: #666666;"">
                                                <strong>Seu codigo de verificacao:</strong>
                                            </p>
                                            <p style=""margin: 10px 0 0 0;"">
                                                <span style=""background-color: #e9ecef; padding: 8px 15px; font-size: 14px; font-family: monospace; letter-spacing: 1px; display: inline-block; word-break: break-all;"">{token}</span>
                                            </p>
                                        </td>
                                    </tr>
                                </table>

                                <!-- Warning -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0;"">
                                    <tr>
                                        <td style=""background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107;"">
                                            <p style=""margin: 0; font-size: 14px; color: #856404;"">
                                                <strong>Este link expira em 2 horas.</strong><br>
                                                Se voce nao solicitou esta redefinicao, ignore este email.
                                            </p>
                                        </td>
                                    </tr>
                                </table>

                                <!-- Divider -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 30px 0;"">
                                    <tr>
                                        <td style=""border-top: 1px solid #e0e0e0;"">&nbsp;</td>
                                    </tr>
                                </table>

                                <!-- Footer -->
                                <p style=""font-size: 12px; color: #999999; text-align: center; margin: 0;"">
                                    Este e um email automatico. Por favor, nao responda.<br>
                                    {DateTime.Now.Year} Employee Management System
                                </p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </body>
    </html>";
    }

        private string GenerateWelcomeEmailBody(string userName, string email, string? temporaryPassword)
        {
            var passwordSection = !string.IsNullOrEmpty(temporaryPassword)
                ? $@"
                                <!-- Password Box -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0;"">
                                    <tr>
                                        <td style=""background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107;"">
                                            <p style=""margin: 0; font-size: 14px; color: #856404;"">
                                                <strong>Sua senha temporaria:</strong><br>
                                                <span style=""background-color: #e9ecef; padding: 5px 10px; font-family: monospace; font-size: 16px;"">{temporaryPassword}</span>
                                            </p>
                                            <p style=""margin: 10px 0 0 0; font-size: 12px; color: #856404;"">
                                                Por seguranca, altere sua senha no primeiro acesso.
                                            </p>
                                        </td>
                                    </tr>
                                </table>"
                : "";

            return $@"<!DOCTYPE html>
    <html lang=""pt-BR"">
    <head>
        <meta charset=""UTF-8"">
        <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"">
        <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
        <title>Bem-vindo</title>
    </head>
    <body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; line-height: 1.6; color: #333333; background-color: #f5f5f5;"">
        <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width: 600px; margin: 0 auto; background-color: #f5f5f5;"">
            <tr>
                <td style=""padding: 20px;"">
                    <!-- Header -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                            <td style=""background-color: #11998e; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
                                <h1 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: bold;"">Bem-vindo!</h1>
                            </td>
                        </tr>
                    </table>

                    <!-- Body -->
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                            <td style=""background-color: #ffffff; padding: 30px; border: 1px solid #e0e0e0; border-top: none; border-radius: 0 0 10px 10px;"">
                                <p style=""font-size: 16px; margin: 0 0 15px 0;"">Ola <strong>{userName}</strong>,</p>

                                <p style=""font-size: 16px; margin: 0 0 25px 0;"">Sua conta foi criada com sucesso no <strong>Employee Management System</strong>!</p>

                                <!-- Email Info Box -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 20px 0;"">
                                    <tr>
                                        <td style=""background-color: #f8f9fa; padding: 15px; border-left: 4px solid #11998e;"">
                                            <p style=""margin: 0; font-size: 14px; color: #666666;"">
                                                <strong>Seu email de acesso:</strong><br>
                                                <span style=""font-size: 16px;"">{email}</span>
                                            </p>
                                        </td>
                                    </tr>
                                </table>

                                {passwordSection}

                                <!-- Button -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                                    <tr>
                                        <td align=""center"" style=""padding: 25px 0;"">
                                            <table cellpadding=""0"" cellspacing=""0"">
                                                <tr>
                                                    <td style=""background-color: #11998e; border-radius: 5px;"">
                                                        <a href=""{_settings.FrontendBaseUrl}/login"" target=""_blank"" style=""display: inline-block; padding: 15px 30px; color: #ffffff; text-decoration: none; font-size: 16px; font-weight: bold;"">Acessar o Sistema</a>
                                                    </td>
                                                </tr>
                                            </table>
                                        </td>
                                    </tr>
                                </table>

                                <!-- Divider -->
                                <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 30px 0;"">
                                    <tr>
                                        <td style=""border-top: 1px solid #e0e0e0;"">&nbsp;</td>
                                    </tr>
                                </table>

                                <!-- Footer -->
                                <p style=""font-size: 12px; color: #999999; text-align: center; margin: 0;"">
                                    Este e um email automatico. Por favor, nao responda.<br>
                                    {DateTime.Now.Year} Employee Management System
                                </p>
                            </td>
                        </tr>
                    </table>
                </td>
            </tr>
        </table>
    </body>
    </html>";
        }
    }
