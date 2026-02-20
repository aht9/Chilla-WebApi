using System.Text.Json;
using Chilla.Domain.Common;
using Microsoft.Extensions.Caching.Distributed;

namespace Chilla.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    
    // تنظیمات استاندارد برای سریالایز کردن جیسون (برای بهینه‌سازی حجم در Redis)
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        IgnoreReadOnlyProperties = false
    };

    public CacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cachedString = await _distributedCache.GetStringAsync(key, cancellationToken);
        
        if (string.IsNullOrWhiteSpace(cachedString))
            return default;

        return JsonSerializer.Deserialize<T>(cachedString, _jsonOptions);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="absoluteExpireTime">در یک زمان مشخص از بین برود</param>
    /// <param name="slidingExpireTime">اگر مدتی به آن درخواستی نیامد منقضی شود</param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    public async Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, TimeSpan? slidingExpireTime = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions();

        // انقضای قطعی (مثلاً دقیقاً 1 ساعت بعد پاک شود)
        if (absoluteExpireTime.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = absoluteExpireTime;
        }

        // انقضای لغزان (مثلاً اگر 20 دقیقه کسی از آن استفاده نکرد پاک شود)
        if (slidingExpireTime.HasValue)
        {
            options.SlidingExpiration = slidingExpireTime;
        }

        var jsonString = JsonSerializer.Serialize(value, _jsonOptions);
        await _distributedCache.SetStringAsync(key, jsonString, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _distributedCache.RemoveAsync(key, cancellationToken);
    }

    public async Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? absoluteExpireTime = null, TimeSpan? slidingExpireTime = null, CancellationToken cancellationToken = default)
    {
        // 1. بررسی وجود در کش
        var cachedValue = await GetAsync<T>(key, cancellationToken);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // 2. اجرا کردن تابع برای دریافت دیتای اصلی (مثلا از دیتابیس)
        var newValue = await factory(cancellationToken);

        // 3. در صورت معتبر بودن دیتا، آن را کش کن
        if (newValue != null)
        {
            await SetAsync(key, newValue, absoluteExpireTime, slidingExpireTime, cancellationToken);
        }

        return newValue;
    }
}