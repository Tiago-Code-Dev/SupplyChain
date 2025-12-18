using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : EntityBaseConfiguration<Employee>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

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

        // Indexes
        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasFilter("IsDeleted = 0");

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
    }
}