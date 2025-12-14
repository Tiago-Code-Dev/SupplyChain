using EmployeeManagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Common.Behaviors;

/// <summary>
/// Behavior para invalidar cache após commands
/// </summary>
public class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheInvalidatorCommand
{
    private readonly ICacheService _cache;
    private readonly ILogger<CacheInvalidationBehavior<TRequest, TResponse>> _logger;

    public CacheInvalidationBehavior(
        ICacheService cache, 
        ILogger<CacheInvalidationBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var result = await next();

        foreach (var key in request.CacheKeysToInvalidate)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache invalidated for key: {Key}", key);
        }

        return result;
    }
}

/// <summary>
/// Interface para commands que invalidam cache
/// </summary>
public interface ICacheInvalidatorCommand
{
    IEnumerable<string> CacheKeysToInvalidate { get; }
}