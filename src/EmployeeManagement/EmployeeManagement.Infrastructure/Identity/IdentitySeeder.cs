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
            try
            {
                if (!await _roleManager.RoleExistsAsync(role.Name!))
                {
                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Role {RoleName} created", role.Name);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create role {RoleName}: {Errors}", 
                            role.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role {RoleName}", role.Name);
                throw;
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@empresa.com";
        const string adminPassword = "Admin@123";

        try
        {
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
                await _userManager.AddToRolesAsync(admin, new[] 
                { 
                    ApplicationRoles.Admin, 
                    ApplicationRoles.Director 
                });

                _logger.LogInformation("Admin user created: {Email}", adminEmail);
            }
            else
            {
                _logger.LogWarning("Failed to create admin user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating admin user");
            throw;
        }
    }
}
