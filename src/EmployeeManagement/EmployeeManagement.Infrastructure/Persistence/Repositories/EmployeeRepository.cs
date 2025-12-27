using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext context) : base(context) { }

    public override async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<Employee?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(e => e.Email.ToLower() == email.ToLower());
        
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> DocumentExistsAsync(string documentNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(e => e.DocumentNumber == documentNumber);
        
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Employee> Employees, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? filterByName = null,
        string? filterByEmail = null,
        Role? filterByRole = null,
        Guid? filterByManagerId = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(searchTerm, filterByName, filterByEmail, filterByRole, filterByManagerId);
        var orderBy = BuildOrderBy(sortBy, sortDescending);

        var result = await GetPagedAsync(
            pageNumber, 
            pageSize, 
            filter, 
            orderBy, 
            "PhoneNumbers,Manager", 
            cancellationToken);

        return (result.Items, result.TotalCount);
    }

    public override Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        // Soft delete: apenas marcar employee como modificado, não mexer em telefones
        if (employee.IsDeleted)
        {
            Context.Entry(employee).State = EntityState.Modified;
            return Task.CompletedTask;
        }

        // Limpar todos os telefones rastreados do ChangeTracker para este employee
        var trackedPhones = Context.ChangeTracker.Entries<PhoneNumber>()
            .Where(e => e.Entity.EmployeeId == employee.Id && e.State == EntityState.Unchanged)
            .ToList();

        // Remover do banco via DbSet diretamente
        foreach (var entry in trackedPhones)
        {
            Context.Set<PhoneNumber>().Remove(entry.Entity);
        }

        // Adicionar novos telefones
        foreach (var phone in employee.PhoneNumbers)
        {
            Context.Set<PhoneNumber>().Add(phone);
        }

        Context.Entry(employee).State = EntityState.Modified;

        return Task.CompletedTask;
    }

    public async Task<bool> HasSubordinatesAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.ManagerId == managerId && !e.IsDeleted, cancellationToken);
    }

    public async Task<Employee?> GetByIdForDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Busca sem Include para evitar problemas de tracking com PhoneNumbers
        return await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await DbSet
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.IsDeleted, true)
                .SetProperty(e => e.DeletedAt, DateTime.UtcNow),
            cancellationToken);
    }

    #region Private Methods

    private static System.Linq.Expressions.Expression<Func<Employee, bool>>? BuildFilter(
        string? searchTerm,
        string? filterByName,
        string? filterByEmail,
        Role? filterByRole,
        Guid? filterByManagerId)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) &&
            string.IsNullOrWhiteSpace(filterByName) &&
            string.IsNullOrWhiteSpace(filterByEmail) &&
            !filterByRole.HasValue &&
            !filterByManagerId.HasValue)
        {
            return null;
        }

        return e =>
            (string.IsNullOrWhiteSpace(searchTerm) || 
                e.FirstName.ToLower().Contains(searchTerm.ToLower()) ||
                e.LastName.ToLower().Contains(searchTerm.ToLower()) ||
                e.Email.ToLower().Contains(searchTerm.ToLower()) ||
                e.DocumentNumber.Contains(searchTerm)) &&
            (string.IsNullOrWhiteSpace(filterByName) || 
                e.FirstName.ToLower().Contains(filterByName.ToLower()) ||
                e.LastName.ToLower().Contains(filterByName.ToLower())) &&
            (string.IsNullOrWhiteSpace(filterByEmail) || 
                e.Email.ToLower().Contains(filterByEmail.ToLower())) &&
            (!filterByRole.HasValue || e.Role == filterByRole.Value) &&
            (!filterByManagerId.HasValue || e.ManagerId == filterByManagerId.Value);
    }

    private static Func<IQueryable<Employee>, IOrderedQueryable<Employee>> BuildOrderBy(
        string? sortBy,
        bool sortDescending)
    {
        return sortBy?.ToLower() switch
        {
            "firstname" => sortDescending 
                ? q => q.OrderByDescending(e => e.FirstName) 
                : q => q.OrderBy(e => e.FirstName),
            "lastname" => sortDescending 
                ? q => q.OrderByDescending(e => e.LastName) 
                : q => q.OrderBy(e => e.LastName),
            "email" => sortDescending 
                ? q => q.OrderByDescending(e => e.Email) 
                : q => q.OrderBy(e => e.Email),
            "role" => sortDescending 
                ? q => q.OrderByDescending(e => e.Role) 
                : q => q.OrderBy(e => e.Role),
            "createdat" => sortDescending 
                ? q => q.OrderByDescending(e => e.CreatedAt) 
                : q => q.OrderBy(e => e.CreatedAt),
            _ => q => q.OrderBy(e => e.FirstName)
        };
    }

    #endregion
}