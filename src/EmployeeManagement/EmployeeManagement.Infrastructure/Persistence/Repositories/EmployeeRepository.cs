using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EmployeeManagement.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;

    public EmployeeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<Employee?> GetByDocumentAsync(string documentNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.DocumentNumber == documentNumber, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.Where(e => e.Email.ToLower() == email.ToLower());
        
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> DocumentExistsAsync(string documentNumber, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees.Where(e => e.DocumentNumber == documentNumber);
        
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.Id != excludeId.Value);
        }
        
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .ToListAsync(cancellationToken);
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
        var query = _context.Employees
            .Include(e => e.PhoneNumbers)
            .Include(e => e.Manager)
            .AsQueryable();

        // Filtro de busca genérico (mantém compatibilidade)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term) ||
                e.DocumentNumber.Contains(term));
        }

        // Filtro específico por nome (FirstName ou LastName)
        if (!string.IsNullOrWhiteSpace(filterByName))
        {
            var nameTerm = filterByName.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(nameTerm) ||
                e.LastName.ToLower().Contains(nameTerm));
        }

        // Filtro específico por email (busca exata ou parcial)
        if (!string.IsNullOrWhiteSpace(filterByEmail))
        {
            var emailTerm = filterByEmail.ToLower();
            query = query.Where(e => e.Email.ToLower().Contains(emailTerm));
        }

        // Filtro específico por permissão/role
        if (filterByRole.HasValue)
        {
            query = query.Where(e => e.Role == filterByRole.Value);
        }

        // Filtro específico por gestor
        if (filterByManagerId.HasValue)
        {
            query = query.Where(e => e.ManagerId == filterByManagerId.Value);
        }

        // Contagem total (após filtros)
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
            "role" => sortDescending 
                ? query.OrderByDescending(e => e.Role) 
                : query.OrderBy(e => e.Role),
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

    public async Task<Employee> AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
        return employee;
    }

    public Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        // Remover telefones existentes (compatível com InMemory e SQL Server)
        var existingPhones = _context.PhoneNumbers
            .Where(p => p.EmployeeId == employee.Id)
            .ToList();
        
        _context.PhoneNumbers.RemoveRange(existingPhones);
        
        // Adicionar novos telefones
        foreach (var phone in employee.PhoneNumbers)
        {
            _context.Entry(phone).State = EntityState.Added;
        }
        
        // Update employee
        _context.Entry(employee).State = EntityState.Modified;
        
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees.FindAsync(new object[] { id }, cancellationToken);
        if (employee is not null)
        {
            _context.Employees.Remove(employee);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> HasSubordinatesAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees.AnyAsync(e => e.ManagerId == managerId, cancellationToken);
    }
}