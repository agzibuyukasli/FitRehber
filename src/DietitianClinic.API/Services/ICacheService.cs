namespace DietitianClinic.API.Services;

/// <summary>
/// Uygulama genelinde kullanılan cache servisinin sözleşmesi.
/// RedisCacheService tarafından implemente edilir.
/// </summary>
public interface ICacheService
{
    /// <summary>Verilen anahtardaki değeri getirir. Yoksa default(T) döner.</summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Verilen anahtara değeri yazar. expiry null ise varsayılan TTL uygulanır.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>Belirli bir anahtarı cache'den siler.</summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Prefix ile başlayan tüm anahtarları siler.
    /// Örnek: "patients:*" → tüm hasta cache'ini temizler.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix);
}
