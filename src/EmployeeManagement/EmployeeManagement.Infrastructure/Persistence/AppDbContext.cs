using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly IPublisher _publisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global Query Filter para Soft Delete
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Converter Delete em Soft Delete
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.Delete();
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
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}