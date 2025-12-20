using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using System.Linq.Expressions;

namespace EmployeeManagement.Domain.Interfaces;

/// <summary>
/// Interface genérica para repositórios
/// </summary>
/// <typeparam name="TEntity">Tipo da entidade</typeparam>
public interface IRepository<TEntity> where TEntity : Entity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca paginada genérica
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? filter = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        string? includeProperties = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Busca com filtro
    /// </summary>
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Conta registros
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken cancellationToken = default);
    
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}