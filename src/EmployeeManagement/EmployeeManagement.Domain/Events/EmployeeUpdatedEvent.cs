using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Events;

public sealed record EmployeeUpdatedEvent(
    Guid EmployeeId,
    string Email) : DomainEvent;