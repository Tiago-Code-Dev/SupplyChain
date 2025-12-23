using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Domain.Interfaces;

public interface ICustomRoleRepository
{
    Task<CustomRole?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<CustomRole?> GetByLegacyRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomRole>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<CustomRole> AddAsync(CustomRole role, CancellationToken cancellationToken = default);
    Task UpdateAsync(CustomRole role, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}