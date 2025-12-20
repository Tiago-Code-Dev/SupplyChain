using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public abstract class EntityBaseConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : Entity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        // Audit Fields - Timestamps
        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt);

        // Audit Fields - User tracking
        builder.Property(e => e.CreatedBy);
        builder.Property(e => e.UpdatedBy);

        // Soft Delete
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);

        // Ignore domain events (não é persistido)
        builder.Ignore(e => e.DomainEvents);

        // Query Filter global para Soft Delete
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Configurações específicas da entidade
        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}