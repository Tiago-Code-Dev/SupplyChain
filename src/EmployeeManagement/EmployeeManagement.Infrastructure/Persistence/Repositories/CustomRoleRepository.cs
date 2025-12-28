using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Persistence.Repositories;

public class CustomRoleRepository : ICustomRoleRepository
{
    private readonly AppDbContext _context;

    public CustomRoleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CustomRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<CustomRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        return await _context.CustomRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name.ToLower() == normalizedName.ToLower(), cancellationToken);
    }

    public async Task<CustomRole?> GetByLegacyRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        var roleName = role.ToString().ToUpperInvariant();
        return await _context.CustomRoles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == roleName && r.IsSystemRole, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomRole>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CustomRoles
            .Include(r => r.Permissions)
            .OrderBy(r => r.HierarchyLevel)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        // Filtro explícito para IsDeleted para garantir que não considera registros soft-deleted
        var query = _context.CustomRoles
            .Where(r => !r.IsDeleted && r.Name.ToLower() == normalizedName.ToLower());

        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<CustomRole> AddAsync(CustomRole role, CancellationToken cancellationToken = default)
    {
        await _context.CustomRoles.AddAsync(role, cancellationToken);
        return role;
    }

    public Task UpdateAsync(CustomRole role, CancellationToken cancellationToken = default)
    {
        _context.CustomRoles.Update(role);
        return Task.CompletedTask;
    }

            public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
            {
                // Usar FirstOrDefaultAsync para respeitar a Query Filter (não deletar registros já deletados)
                var role = await _context.CustomRoles
                    .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

                if (role is not null)
                {
                    // Fazer soft delete diretamente em vez de usar Remove()
                    // Isso evita problemas com o interceptor de SaveChangesAsync
                    role.Delete();
                }
            }
        }