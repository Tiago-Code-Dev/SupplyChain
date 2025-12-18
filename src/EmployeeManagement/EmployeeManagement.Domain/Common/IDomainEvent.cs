using MediatR;

namespace EmployeeManagement.Domain.Common;

/// <summary>
/// Marker interface para Domain Events
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base class para Domain Events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}