using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositório específico para Employee
/// </summary>
public class EmployeeRepository : Repository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(AppDbContext context) : base(context)
    {
    }

    public override async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public override async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .ToListAsync(cancellationToken);
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

    public async Task<(IEnumerable<Employee> Employees, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .AsQueryable();

        // Filtro de busca
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.DocumentNumber.Contains(term));
        }

        // Contagem total
        var totalCount = await query.CountAsync(cancellationToken);

        // Ordenação
        query = sortBy?.ToLower() switch
        {
            "firstname" => sortDescending
                ? query.OrderByDescending(e => e.FirstName)
                : query.OrderBy(e => e.FirstName),
            "lastname" => sortDescending
                ? query.OrderByDescending(e => e.LastName)
                : query.OrderBy(e => e.LastName),
            "email" => sortDescending
                ? query.OrderByDescending(e => e.Email)
                : query.OrderBy(e => e.Email),
            "createdat" => sortDescending
                ? query.OrderByDescending(e => e.CreatedAt)
                : query.OrderBy(e => e.CreatedAt),
            _ => query.OrderBy(e => e.FirstName)
        };

        // Paginação
        var employees = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (employees, totalCount);
    }

    public override async Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        // Remover telefones antigos
        await Context.PhoneNumbers
            .Where(p => p.EmployeeId == employee.Id)
            .ExecuteDeleteAsync(cancellationToken);

        // Limpar tracking de telefones
        var trackedPhones = Context.ChangeTracker.Entries<PhoneNumber>()
            .Where(e => e.Entity.EmployeeId == employee.Id)
            .ToList();

        foreach (var entry in trackedPhones)
        {
            entry.State = EntityState.Detached;
        }

        // Adicionar novos telefones
        foreach (var phone in employee.PhoneNumbers)
        {
            Context.Entry(phone).State = EntityState.Added;
        }

        Context.Entry(employee).State = EntityState.Modified;
    }
}