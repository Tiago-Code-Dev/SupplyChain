using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Enums;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed record GetAllEmployeesQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? FilterByName = null,
    string? FilterByEmail = null,
    Role? FilterByRole = null,
    Guid? FilterByManagerId = null,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedResult<EmployeeResponse>>;