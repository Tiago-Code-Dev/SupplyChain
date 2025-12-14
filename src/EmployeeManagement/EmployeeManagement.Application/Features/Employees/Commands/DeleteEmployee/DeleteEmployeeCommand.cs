using EmployeeManagement.Application.Common.Interfaces;

namespace EmployeeManagement.Application.Features.Employees.Commands.DeleteEmployee;

public sealed record DeleteEmployeeCommand(Guid Id) : ICommand;