using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Features.Employees.Common;

public sealed record EmployeeResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string DocumentNumber,
    DateTime BirthDate,
    Role Role,
    Guid? ManagerId,
    string? ManagerName,
    IReadOnlyList<string> PhoneNumbers,
    // Audit Fields
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime? UpdatedAt,
    Guid? UpdatedBy)
{
    public static EmployeeResponse FromEntity(Employee employee) => new(
        employee.Id,
        employee.FirstName,
        employee.LastName,
        employee.FullName,
        employee.Email,
        employee.DocumentNumber,
        employee.BirthDate,
        employee.Role,
        employee.ManagerId,
        employee.Manager?.FullName,
        employee.PhoneNumbers.Select(p => p.Number).ToList(),
        // Audit Fields
        employee.CreatedAt,
        employee.CreatedBy,
        employee.UpdatedAt,
        employee.UpdatedBy);
}