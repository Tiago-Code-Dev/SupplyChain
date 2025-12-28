using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public class CustomRoleConfiguration : IEntityTypeConfiguration<CustomRole>
{
    public void Configure(EntityTypeBuilder<CustomRole> builder)
    {
        builder.ToTable("CustomRoles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.HierarchyLevel)
            .IsRequired();

        builder.Property(r => r.IsSystemRole)
            .IsRequired();

        builder.Property(r => r.LegacyRole)
            .HasConversion<int?>();

        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(r => r.LegacyRole)
            .IsUnique()
            .HasFilter("[LegacyRole] IS NOT NULL");

        // Seed dos 4 roles do sistema
        builder.HasData(
            CustomRole.CreateForSeed(
                new Guid("11111111-1111-1111-1111-111111111111"),
                "Employee", "Funcionário", 10, Role.Employee),
            CustomRole.CreateForSeed(
                new Guid("22222222-2222-2222-2222-222222222222"),
                "Leader", "Líder", 20, Role.Leader),
            CustomRole.CreateForSeed(
                new Guid("33333333-3333-3333-3333-333333333333"),
                "Director", "Diretor", 30, Role.Director),
            CustomRole.CreateForSeed(
                new Guid("44444444-4444-4444-4444-444444444444"),
                "Admin", "Administrador", 100, Role.Admin)
        );
    }
}