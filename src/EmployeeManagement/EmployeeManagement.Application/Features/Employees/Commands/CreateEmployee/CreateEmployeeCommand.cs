using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Features.Employees.Commands.CreateEmployee;

public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string Email,
    string DocumentNumber,
    DateTime BirthDate,
    string Password,
    Role Role,
    Guid? ManagerId,
    List<string> PhoneNumbers,
    Role CurrentUserRole) : ICommand<EmployeeResponse>;