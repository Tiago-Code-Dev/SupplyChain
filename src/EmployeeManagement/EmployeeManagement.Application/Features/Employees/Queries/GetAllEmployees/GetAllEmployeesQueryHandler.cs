using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Interfaces;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed class GetAllEmployeesQueryHandler
    : IQueryHandler<GetAllEmployeesQuery, PagedResult<EmployeeResponse>>
{
    private readonly IEmployeeRepository _repository;

    public GetAllEmployeesQueryHandler(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<EmployeeResponse>> Handle(
        GetAllEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var (employees, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.SortBy,
            request.SortDescending,
            cancellationToken);

        var items = employees
            .Select(EmployeeResponse.FromEntity)
            .ToList();

        return PagedResult<EmployeeResponse>.Create(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}