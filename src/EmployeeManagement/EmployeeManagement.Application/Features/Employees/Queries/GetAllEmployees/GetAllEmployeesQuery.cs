using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Domain.Common;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed record GetAllEmployeesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedResult<EmployeeResponse>>;