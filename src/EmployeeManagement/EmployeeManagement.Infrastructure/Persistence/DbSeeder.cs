using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<AppDbContext>>();
        
        try
        {
            // Check if admin exists
            var adminExists = await context.Employees
                .AnyAsync(e => e.Email == "admin@empresa.com");
            
            if (adminExists)
            {
                logger.LogInformation("Admin user already exists, skipping seed.");
                return;
            }

            var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();
            var hashedPassword = passwordHasher.Hash("Admin@123");

            // Passar telefones no Create para satisfazer a validação do domínio
            var adminResult = Employee.Create(
                firstName: "Admin",
                lastName: "System",
                email: "admin@empresa.com",
                documentNumber: "00000000000",
                birthDate: DateTime.UtcNow.AddYears(-30),
                passwordHash: hashedPassword,
                role: Role.Director,
                managerId: null,
                phoneNumbers: ["11999999999"]); // Telefone obrigatório

            if (adminResult.IsFailure)
            {
                logger.LogError("Failed to create admin user: {Error}", adminResult.Error.Description);
                throw new InvalidOperationException($"Failed to create admin: {adminResult.Error.Description}");
            }

            var admin = adminResult.Value;

            await context.Employees.AddAsync(admin);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Admin user created successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }
}