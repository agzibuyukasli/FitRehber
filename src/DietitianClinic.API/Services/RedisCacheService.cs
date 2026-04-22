using System.Text.Json;
using StackExchange.Redis;

namespace DietitianClinic.API.Services;

/// <summary>
/// Cache-Aside pattern uygulayan Redis cache servisi.
/// Redis bağlantısı kopuk olsa bile uygulama çalışmaya devam eder (graceful degradation).
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly TimeSpan _defaultExpiry;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;
        _defaultExpiry = TimeSpan.FromMinutes(
            configuration.GetValue<int>("Redis:DefaultExpiryMinutes", 5));
    }

    private IDatabase Db => _redis.GetDatabase();

    /// <inheritdoc/>
    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await Db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return default;

            return JsonSerializer.Deserialize<T>(value.ToString());
        }
        catch (Exception ex)
        {
            // Redis hata verirse log at, null dön → uygulama SQL'den okur
            _logger.LogWarning(ex, "Redis GET hatası. Key: {Key}", key);
            return default;
        }
    }

    /// <inheritdoc/>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            await Db.StringSetAsync(key, serialized, expiry ?? _defaultExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET hatası. Key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key)
    {
        try
        {
            await Db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis DELETE hatası. Key: {Key}", key);
        }
    }

    /// <inheritdoc/>
    public async Task RemoveByPrefixAsync(string prefix)
    {
        try
        {
            // Tüm Redis node'larından prefix'e uyan key'leri bul ve sil
            var server = _redis.GetServers().FirstOrDefault();
            if (server == null) return;

            var keys = server.Keys(pattern: $"{prefix}*").ToArray();
            if (keys.Length > 0)
                await Db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis PREFIX DELETE hatası. Prefix: {Prefix}", prefix);
        }
    }
}
