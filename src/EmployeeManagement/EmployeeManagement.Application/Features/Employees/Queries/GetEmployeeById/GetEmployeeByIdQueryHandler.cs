using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.Common.Interfaces;
using EmployeeManagement.Application.Features.Employees.Common;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Interfaces;

namespace EmployeeManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeResponse?>
{
    private readonly IEmployeeRepository _repository;
    private readonly ICacheService _cache;

    public GetEmployeeByIdQueryHandler(
        IEmployeeRepository repository,
        ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<EmployeeResponse?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Employee(request.Id);

        // Tentar obter do cache primeiro
        var cached = await _cache.GetAsync<EmployeeResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        // Se não estiver no cache, buscar do banco
        var employee = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (employee is null)
        {
            return null;
        }

        var response = EmployeeResponse.FromEntity(employee);

        // Salvar no cache
        await _cache.SetAsync(cacheKey, response, TimeSpan.FromMinutes(10), cancellationToken);

        return response;
    }
}