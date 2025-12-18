using EmployeeManagement.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Events;

public sealed class PasswordChangedEventHandler : INotificationHandler<PasswordChangedEvent>
{
    private readonly ILogger<PasswordChangedEventHandler> _logger;

    public PasswordChangedEventHandler(ILogger<PasswordChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PasswordChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Password changed for employee: {EmployeeId} at {OccurredOn}",
            notification.EmployeeId,
            notification.OccurredOn);

        // Aqui você pode:
        // - Enviar email de notificação de mudança de senha
        // - Invalidar outras sessões
        // - Registrar em auditoria de segurança

        return Task.CompletedTask;
    }
}