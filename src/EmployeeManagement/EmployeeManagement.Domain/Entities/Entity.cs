using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Domain.Entities;

public abstract class Entity
{
    private static TimeProvider _timeProvider = TimeProvider.System;

    public static void SetTimeProvider(TimeProvider timeProvider) =>
        _timeProvider = timeProvider;

    public Guid Id { get; protected set; } = Guid.NewGuid();
    
    // Audit Fields - Timestamps
    public DateTime CreatedAt { get; protected set; } = _timeProvider.GetUtcNow().UtcDateTime;
    public DateTime? UpdatedAt { get; protected set; }

    // Audit Fields - User tracking
    public Guid? CreatedBy { get; protected set; }
    public Guid? UpdatedBy { get; protected set; }

    // Soft Delete with audit
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void SetCreatedBy(Guid? userId)
    {
        CreatedBy = userId;
    }

    public void SetUpdatedBy(Guid? userId)
    {
        UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        UpdatedBy = userId;
    }

    protected void SetUpdatedAt() => UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public virtual void Delete(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        DeletedBy = deletedBy;
    }

    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}