using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeByEmail;

public sealed record GetEmployeeByEmailQuery(string Email) : IQuery<EmployeeResponse?>;
