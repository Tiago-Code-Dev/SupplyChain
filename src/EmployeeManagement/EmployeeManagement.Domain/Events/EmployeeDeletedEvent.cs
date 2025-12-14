using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Events;

public sealed record EmployeeDeletedEvent(
    Guid EmployeeId,
    string Email) : DomainEvent;