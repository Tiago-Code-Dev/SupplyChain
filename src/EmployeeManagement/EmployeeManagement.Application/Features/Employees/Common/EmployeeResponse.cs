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
    DateTime CreatedAt,
    DateTime? UpdatedAt)
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
        employee.CreatedAt,
        employee.UpdatedAt);
}