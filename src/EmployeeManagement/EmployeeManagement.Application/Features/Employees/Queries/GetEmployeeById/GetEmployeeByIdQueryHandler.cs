using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler 
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse?>
{
    private readonly IEmployeeRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetEmployeeByIdQueryHandler> _logger;

    public GetEmployeeByIdQueryHandler(
        IEmployeeRepository repository,
        ICacheService cache,
        ILogger<GetEmployeeByIdQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EmployeeResponse?> Handle(
        GetEmployeeByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Employee(request.Id);

        var response = await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Fetching employee {Id} from database", request.Id);
                var employee = await _repository.GetByIdAsync(request.Id, cancellationToken);
                return employee is null ? null : EmployeeResponse.FromEntity(employee);
            },
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return response;
    }
}