using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Chilla.Infrastructure.Services;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        // 1. دریافت بایت‌ها از Redis
        var bytes = await _distributedCache.GetAsync(key, cancellationToken);
        if (bytes == null) return default;

        // 2. Decompress (باز کردن فشرده‌سازی)
        using var outputStream = new MemoryStream();
        using (var inputStream = new MemoryStream(bytes))
        using (var gZipStream = new GZipStream(inputStream, CompressionMode.Decompress))
        {
            await gZipStream.CopyToAsync(outputStream, cancellationToken);
        }

        // 3. Deserialize (تبدیل به آبجکت)
        var json = System.Text.Encoding.UTF8.GetString(outputStream.ToArray());
        return JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        // 1. Serialize (تبدیل به JSON)
        var json = JsonSerializer.Serialize(value);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        // 2. Compress (فشرده‌سازی GZip)
        using var outputStream = new MemoryStream();
        using (var gZipStream = new GZipStream(outputStream, CompressionLevel.Optimal))
        {
            await gZipStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }
        var compressedBytes = outputStream.ToArray();

        // 3. تنظیمات انقضا
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;

        // 4. ذخیره در Redis
        await _distributedCache.SetAsync(key, compressedBytes, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }
}