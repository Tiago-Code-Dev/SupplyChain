using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Domain.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Employee?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica se existe outro funcionário com o mesmo email (excluindo um ID específico)
    /// </summary>
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica se existe outro funcionário com o mesmo documento (excluindo um ID específico)
    /// </summary>
    Task<bool> DocumentExistsAsync(string documentNumber, Guid? excludeId = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retorna funcionários paginados com filtros específicos
    /// </summary>
    /// <param name="pageNumber">Número da página</param>
    /// <param name="pageSize">Tamanho da página</param>
    /// <param name="searchTerm">Termo de busca genérico (nome, email, documento)</param>
    /// <param name="filterByName">Filtro específico por nome</param>
    /// <param name="filterByEmail">Filtro específico por email</param>
    /// <param name="filterByRole">Filtro específico por permissão/role</param>
    /// <param name="filterByManagerId">Filtro específico por gestor</param>
    /// <param name="sortBy">Campo para ordenação</param>
    /// <param name="sortDescending">Ordenar de forma decrescente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    Task<(IEnumerable<Employee> Employees, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? filterByName = null,
        string? filterByEmail = null,
        Role? filterByRole = null,
        Guid? filterByManagerId = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default);
    
    Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Verifica se o funcionário possui subordinados (funcionários que o têm como gestor)
    /// </summary>
    Task<bool> HasSubordinatesAsync(Guid managerId, CancellationToken cancellationToken = default);
}