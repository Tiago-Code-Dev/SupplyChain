using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Common;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetAllEmployees;

public sealed class GetAllEmployeesQueryHandler
    : IQueryHandler<GetAllEmployeesQuery, PagedResult<EmployeeResponse>>
{
    private readonly IEmployeeRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetAllEmployeesQueryHandler> _logger;

    public GetAllEmployeesQueryHandler(
        IEmployeeRepository repository,
        ICacheService cache,
        ILogger<GetAllEmployeesQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<EmployeeResponse>> Handle(
        GetAllEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        // Cache apenas para listagens sem filtro (mais comuns)
        var hasFilters = !string.IsNullOrEmpty(request.SearchTerm) ||
                         !string.IsNullOrEmpty(request.FilterByName) ||
                         !string.IsNullOrEmpty(request.FilterByEmail) ||
                         request.FilterByRole.HasValue ||
                         request.FilterByManagerId.HasValue ||
                         !string.IsNullOrEmpty(request.SortBy);
        
        var cacheKey = CacheKeys.EmployeesList(request.PageNumber, request.PageSize);

        if (!hasFilters)
        {
            var cached = await _cache.GetAsync<PagedResult<EmployeeResponse>>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Returning employees list from cache");
                return cached;
            }
        }

        _logger.LogInformation(
            "Fetching employees from database. Page: {Page}, Size: {Size}, Search: {Search}, FilterByName: {FilterByName}, FilterByEmail: {FilterByEmail}, FilterByRole: {FilterByRole}, FilterByManagerId: {FilterByManagerId}",
            request.PageNumber, request.PageSize, request.SearchTerm, 
            request.FilterByName, request.FilterByEmail, request.FilterByRole, request.FilterByManagerId);

        var (employees, totalCount) = await _repository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.FilterByName,
            request.FilterByEmail,
            request.FilterByRole,
            request.FilterByManagerId,
            request.SortBy,
            request.SortDescending,
            cancellationToken);

        var items = employees
            .Select(EmployeeResponse.FromEntity)
            .ToList();

        var result = PagedResult<EmployeeResponse>.Create(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize);

        // Cache apenas listagens sem filtro
        if (!hasFilters)
        {
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);
        }

        return result;
    }
}