using EmployeeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmployeeManagement.Infrastructure.Persistence.Configurations;

public class PhoneNumberConfiguration : EntityBaseConfiguration<PhoneNumber>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PhoneNumber> builder)
    {
        builder.ToTable("PhoneNumbers");

        builder.Property(p => p.Number)
            .IsRequired()
            .HasMaxLength(20);
    }
}