using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Permission)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Resource)
            .HasMaxLength(100);

        builder.HasOne(p => p.CustomRole)
            .WithMany(r => r.Permissions)
            .HasForeignKey(p => p.CustomRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.CustomRoleId, p.Permission, p.Resource })
            .IsUnique();
    }
}