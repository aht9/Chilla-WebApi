namespace Chilla.Domain.Common;
public interface ICacheService
{
    /// <summary>
    /// دریافت اطلاعات از کش با استفاده از کلید
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// ذخیره اطلاعات در کش با امکان تنظیم زمان انقضا
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? absoluteExpireTime = null, TimeSpan? slidingExpireTime = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// حذف یک آیتم از کش
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// الگوی هوشمند: اگر در کش بود برمی‌گرداند، اگر نبود تابع factory را اجرا کرده، نتیجه را کش می‌کند و سپس برمی‌گرداند
    /// </summary>
    Task<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? absoluteExpireTime = null, TimeSpan? slidingExpireTime = null, CancellationToken cancellationToken = default);
}