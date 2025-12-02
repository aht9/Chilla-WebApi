using Microsoft.Extensions.Caching.Distributed;

namespace Chilla.Infrastructure.Authentication;

public interface IOtpService
{
    Task<string> GenerateAndCacheOtpAsync(string phoneNumber, int expiryMinutes = 2);
    Task<bool> ValidateOtpAsync(string phoneNumber, string code);
}

public class OtpService : IOtpService
{
    private readonly IDistributedCache _cache;

    public OtpService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GenerateAndCacheOtpAsync(string phoneNumber, int expiryMinutes = 2)
    {
        // تولید کد تصادفی امن (در پروداکشن از کلاس RandomNumberGenerator استفاده کنید)
        var code = Random.Shared.Next(10000, 99999).ToString();
        
        var key = $"otp:{phoneNumber}";
        
        await _cache.SetStringAsync(key, code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiryMinutes)
        });

        return code;
    }

    public async Task<bool> ValidateOtpAsync(string phoneNumber, string code)
    {
        var key = $"otp:{phoneNumber}";
        var cachedCode = await _cache.GetStringAsync(key);

        if (cachedCode == code)
        {
            await _cache.RemoveAsync(key); // یکبار مصرف بودن
            return true;
        }

        return false;
    }
}