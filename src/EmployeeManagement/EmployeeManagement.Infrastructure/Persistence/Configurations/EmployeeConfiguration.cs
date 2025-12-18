using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.DocumentNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.PasswordHash)
            .IsRequired();

        builder.Property(e => e.Role)
            .IsRequired();

        // Soft Delete columns
        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt);

        // Indexes
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasFilter("IsDeleted = 0"); // Unique apenas para não deletados

        builder.HasIndex(e => e.DocumentNumber)
            .IsUnique()
            .HasFilter("IsDeleted = 0");

        builder.HasIndex(e => e.IsDeleted);

        // Relationships
        builder.HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.PhoneNumbers)
            .WithOne(p => p.Employee)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}