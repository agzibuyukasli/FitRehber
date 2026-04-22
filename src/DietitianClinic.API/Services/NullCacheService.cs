namespace DietitianClinic.API.Services;

/// <summary>
/// Redis mevcut olmadığında kullanılan no-op implementasyon.
/// Her işlem sessizce başarılı olur; GET her zaman null döner.
/// </summary>
internal sealed class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => Task.FromResult(default(T?));
    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) => Task.CompletedTask;
    public Task RemoveAsync(string key) => Task.CompletedTask;
    public Task RemoveByPrefixAsync(string prefix) => Task.CompletedTask;
}
