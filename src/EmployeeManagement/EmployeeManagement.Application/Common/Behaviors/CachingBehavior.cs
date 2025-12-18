using EmployeeManagement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Application.Common.Behaviors;

/// <summary>
/// Behavior para cache automático de queries
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        
        var cachedResult = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        
        if (cachedResult is not null)
        {
            _logger.LogDebug("Returning cached result for {CacheKey}", cacheKey);
            return cachedResult;
        }

        var result = await next();

        if (result is not null)
        {
            await _cache.SetAsync(cacheKey, result, request.CacheExpiration, cancellationToken);
            _logger.LogDebug("Cached result for {CacheKey}", cacheKey);
        }

        return result;
    }
}

/// <summary>
/// Interface para queries que devem ser cacheadas
/// </summary>
public interface ICachedQuery
{
    string CacheKey { get; }
    TimeSpan? CacheExpiration { get; }
}