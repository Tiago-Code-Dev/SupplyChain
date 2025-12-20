using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeByEmail;

public sealed class GetEmployeeByEmailQueryHandler 
    : IQueryHandler<GetEmployeeByEmailQuery, EmployeeResponse?>
{
    private readonly IEmployeeRepository _repository;
    private readonly ICacheService _cache;
    private readonly ILogger<GetEmployeeByEmailQueryHandler> _logger;

    public GetEmployeeByEmailQueryHandler(
        IEmployeeRepository repository,
        ICacheService cache,
        ILogger<GetEmployeeByEmailQueryHandler> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EmployeeResponse?> Handle(
        GetEmployeeByEmailQuery request, 
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.EmployeeByEmail(request.Email);

        var response = await _cache.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                _logger.LogInformation("Fetching employee with email {Email} from database", request.Email);
                var employee = await _repository.GetByEmailAsync(request.Email, cancellationToken);
                return employee is null ? null : EmployeeResponse.FromEntity(employee);
            },
            TimeSpan.FromMinutes(5),
            cancellationToken);

        return response;
    }
}
