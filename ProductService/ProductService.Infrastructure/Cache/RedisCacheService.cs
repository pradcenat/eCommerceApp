using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ProductService.Infrastructure.Cache
{
    /// <summary>
    /// Redis Cache Service — implements ICacheService using StackExchange.Redis
    /// All product data is cached here to reduce database load
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RedisCacheService> _logger;
        private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(30);

        public RedisCacheService(
            IConnectionMultiplexer connectionMultiplexer,
            ILogger<RedisCacheService> logger)
        {
            _database = connectionMultiplexer.GetDatabase();
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _database.StringGetAsync(key);
                if (value.IsNullOrEmpty)
                    return default;

                _logger.LogDebug("Cache hit: {Key}", key);
                return JsonSerializer.Deserialize<T>(value!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis GET error for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var serialized = JsonSerializer.Serialize(value);
                await _database.StringSetAsync(
                    key,
                    serialized,
                    expiry ?? DefaultExpiry);

                _logger.LogDebug("Cache set: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis SET error for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _database.KeyDeleteAsync(key);
                _logger.LogDebug("Cache removed: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis DELETE error for key: {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _database.KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis EXISTS error for key: {Key}", key);
                return false;
            }
        }
    }
}
