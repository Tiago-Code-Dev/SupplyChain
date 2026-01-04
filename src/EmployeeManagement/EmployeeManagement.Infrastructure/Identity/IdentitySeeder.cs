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
        await SeedDirectorUserAsync();
        await SeedLeaderUserAsync();
        await SeedEmployeeUserAsync();
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

    private async Task SeedDirectorUserAsync()
    {
        const string directorEmail = "director@empresa.com";
        const string directorPassword = "Director@123";

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(directorEmail);
            if (existingUser != null)
            {
                _logger.LogInformation("Director user already exists");
                return;
            }

            var director = new ApplicationUser
            {
                Email = directorEmail,
                UserName = directorEmail,
                FirstName = "Maria",
                LastName = "Diretora",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(director, directorPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(director, ApplicationRoles.Director);
                _logger.LogInformation("Director user created: {Email}", directorEmail);
            }
            else
            {
                _logger.LogWarning("Failed to create director user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating director user");
            throw;
        }
    }

    private async Task SeedLeaderUserAsync()
    {
        const string leaderEmail = "leader@empresa.com";
        const string leaderPassword = "Leader@123";

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(leaderEmail);
            if (existingUser != null)
            {
                _logger.LogInformation("Leader user already exists");
                return;
            }

            var leader = new ApplicationUser
            {
                Email = leaderEmail,
                UserName = leaderEmail,
                FirstName = "João",
                LastName = "Líder",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(leader, leaderPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(leader, ApplicationRoles.Leader);
                _logger.LogInformation("Leader user created: {Email}", leaderEmail);
            }
            else
            {
                _logger.LogWarning("Failed to create leader user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating leader user");
            throw;
        }
    }

    private async Task SeedEmployeeUserAsync()
    {
        const string employeeEmail = "employee@empresa.com";
        const string employeePassword = "Employee@123";

        try
        {
            var existingUser = await _userManager.FindByEmailAsync(employeeEmail);
            if (existingUser != null)
            {
                _logger.LogInformation("Employee user already exists");
                return;
            }

            var employee = new ApplicationUser
            {
                Email = employeeEmail,
                UserName = employeeEmail,
                FirstName = "Carlos",
                LastName = "Funcionário",
                EmailConfirmed = true,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(employee, employeePassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(employee, ApplicationRoles.Employee);
                _logger.LogInformation("Employee user created: {Email}", employeeEmail);
            }
            else
            {
                _logger.LogWarning("Failed to create employee user: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating employee user");
            throw;
        }
    }
}
