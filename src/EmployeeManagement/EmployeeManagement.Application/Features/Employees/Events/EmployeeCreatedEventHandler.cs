using EmployeeManagement.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Events;

public sealed class EmployeeCreatedEventHandler : INotificationHandler<EmployeeCreatedEvent>
{
    private readonly ILogger<EmployeeCreatedEventHandler> _logger;

    public EmployeeCreatedEventHandler(ILogger<EmployeeCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(EmployeeCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Employee created: {EmployeeId} - {FullName} ({Email}) at {OccurredOn}",
            notification.EmployeeId,
            notification.FullName,
            notification.Email,
            notification.OccurredOn);

        // Aqui você pode:
        // - Enviar email de boas-vindas
        // - Notificar sistemas externos
        // - Criar registros de auditoria
        // - etc.

        return Task.CompletedTask;
    }
}