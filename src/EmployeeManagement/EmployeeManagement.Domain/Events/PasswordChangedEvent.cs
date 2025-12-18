using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Events;

public sealed record PasswordChangedEvent(
    Guid EmployeeId) : DomainEvent;