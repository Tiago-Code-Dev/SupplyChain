using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Repositório específico para Employee com métodos adicionais
/// </summary>
public interface IEmployeeRepository : IRepository<Employee>
{
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Employee?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default);
    
    Task<(IEnumerable<Employee> Employees, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
}