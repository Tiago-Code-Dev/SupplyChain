using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Identity;

/// <summary>
/// Seeder para dados iniciais do Identity
/// </summary>
public class IdentitySeeder
{
    private readonly AppIdentityDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        AppIdentityDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger<IdentitySeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Aplicar migrações
            await _context.Database.MigrateAsync();
        }
        catch
        {
            // Se for InMemory, apenas garante criação
            await _context.Database.EnsureCreatedAsync();
        }

        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new ApplicationRole(ApplicationRoles.Employee, "Funcionário comum"),
            new ApplicationRole(ApplicationRoles.Leader, "Líder de equipe"),
            new ApplicationRole(ApplicationRoles.Director, "Diretor"),
            new ApplicationRole(ApplicationRoles.Admin, "Administrador do sistema")
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role.Name!))
            {
                await _roleManager.CreateAsync(role);
                _logger.LogInformation("Role {RoleName} created", role.Name);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@empresa.com";
        const string adminPassword = "Admin@123";

        var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin user already exists");
            return;
        }

        var admin = new ApplicationUser
        {
            Email = adminEmail,
            UserName = adminEmail,
            FirstName = "Admin",
            LastName = "System",
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            // Adicionar todas as roles ao admin
            await _userManager.AddToRolesAsync(admin, new[] 
            { 
                ApplicationRoles.Admin, 
                ApplicationRoles.Director 
            });

            _logger.LogInformation("Admin user created: {Email}", adminEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create admin user: {Errors}", errors);
        }
    }
}
