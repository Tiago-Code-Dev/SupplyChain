using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Events;

public sealed record EmployeeCreatedEvent(
    Guid EmployeeId,
    string Email,
    string FullName) : DomainEvent;