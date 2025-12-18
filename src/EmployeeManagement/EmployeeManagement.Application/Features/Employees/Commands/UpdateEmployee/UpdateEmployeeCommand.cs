using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Features.Employees.Commands.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    DateTime BirthDate,
    Guid? ManagerId,
    List<string> PhoneNumbers,
    Role? NewRole,
    Role CurrentUserRole) : ICommand<EmployeeResponse>;