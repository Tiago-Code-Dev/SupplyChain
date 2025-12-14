using System.Text.Json;
using EmployeeManagement.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Infrastructure.Caching;

/// <summary>
/// Implementação do cache usando Redis
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key, cancellationToken);

            if (string.IsNullOrEmpty(cachedValue))
            {
                _logger.LogDebug("Cache MISS for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache HIT for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting cache for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(30)
            };

            var serializedValue = JsonSerializer.Serialize(value, JsonOptions);
            await _cache.SetStringAsync(key, serializedValue, options, cancellationToken);

            _logger.LogDebug("Cache SET for key: {Key}, expiration: {Expiration}", key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting cache for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _logger.LogDebug("Cache REMOVED for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing cache for key: {Key}", key);
        }
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // Redis via IDistributedCache não suporta remoção por prefixo
        // Para isso seria necessário usar StackExchange.Redis diretamente
        _logger.LogDebug("RemoveByPrefix called for prefix: {Prefix} (not supported via IDistributedCache)", prefix);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cachedValue = await GetAsync<T>(key, cancellationToken);

        if (cachedValue is not null)
        {
            return cachedValue;
        }

        var value = await factory();

        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }
}