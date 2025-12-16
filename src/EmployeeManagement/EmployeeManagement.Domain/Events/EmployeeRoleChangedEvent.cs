using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Domain.Events;

public sealed record EmployeeRoleChangedEvent(
    Guid EmployeeId,
    Role OldRole,
    Role NewRole) : DomainEvent;