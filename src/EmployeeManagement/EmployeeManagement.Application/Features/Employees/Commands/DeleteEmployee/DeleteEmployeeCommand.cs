using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

public sealed record DeleteEmployeeCommand(Guid Id, Role CurrentUserRole) : ICommand;