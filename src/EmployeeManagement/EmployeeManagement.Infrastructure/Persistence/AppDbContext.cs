using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;
    private readonly ICurrentUserService? _currentUserService;
    private readonly ILogger<AppDbContext>? _logger;

    public AppDbContext(
        DbContextOptions<AppDbContext> options, 
        IPublisher publisher,
        ICurrentUserService? currentUserService = null,
        ILogger<AppDbContext>? logger = null)
        : base(options)
    {
        _publisher = publisher;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();
    public DbSet<CustomRole> CustomRoles => Set<CustomRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filter para Soft Delete
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService?.UserId;

        // Aplicar auditoria automaticamente
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreatedBy(currentUserId);
                    break;

                case EntityState.Modified:
                    entry.Entity.SetUpdatedBy(currentUserId);
                    break;

                case EntityState.Deleted:
                    // Converter Delete em Soft Delete com auditoria
                    entry.State = EntityState.Modified;
                    entry.Entity.Delete(currentUserId);
                    break;
            }
        }

        // Coletar domain events antes de salvar
        var domainEvents = ChangeTracker.Entries<Entity>()
            .SelectMany(entry => entry.Entity.DomainEvents)
            .ToList();

        // Limpar eventos das entidades
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            entry.Entity.ClearDomainEvents();
        }

        // Salvar alterações
        var result = await base.SaveChangesAsync(cancellationToken);

        // Publicar eventos após salvar com sucesso
        // CORREÇÃO: Não falhar a operação principal se a publicação de evento falhar
        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                // Log o erro mas não falhar - os dados já foram salvos
                _logger?.LogWarning(ex, 
                    "Failed to publish domain event {EventType}. Data was saved successfully.", 
                    domainEvent.GetType().Name);
            }
        }

        return result;
    }
}