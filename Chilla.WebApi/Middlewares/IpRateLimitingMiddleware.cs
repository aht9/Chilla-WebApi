using Chilla.Domain.Aggregates.SecurityAggregate;
using Chilla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chilla.WebApi.Middlewares;

public class IpRateLimitingMiddleware : IMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IpRateLimitingMiddleware> _logger;

    // کانفیگ: حداکثر ۱۰۰ درخواست در دقیقه
    private const int MaxRequestsPerMinute = 100;

    public IpRateLimitingMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory,
        ILogger<IpRateLimitingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }


    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // ۱. چک کردن اینکه آیا IP هم‌اکنون بلاک است؟
            // از AsNoTracking برای سرعت استفاده می‌کنیم
            var isBlocked = await dbContext.BlockedIps
                .AnyAsync(b => b.IpAddress == ipAddress &&
                               (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow) &&
                               !b.IsDeleted);

            if (isBlocked)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Your IP has been blocked due to suspicious activity.");
                return;
            }

            // ۲. ثبت لاگ درخواست (بهتر است این کار را در یک Background Task یا Fire-and-Forget انجام دهید تا سرعت کاربر کم نشود)
            // اما برای سادگی اینجا مستقیم می‌نویسیم.
            var log = new RequestLog(ipAddress, context.Request.Path);
            dbContext.RequestLogs.Add(log);

            // ۳. بررسی تعداد درخواست‌ها در ۱ دقیقه اخیر
            var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
            var requestCount = await dbContext.RequestLogs
                .CountAsync(r => r.IpAddress == ipAddress && r.OccurredOn >= oneMinuteAgo);

            if (requestCount > MaxRequestsPerMinute)
            {
                // ۴. بلاک کردن IP
                var blockedIp = new BlockedIp(ipAddress, "Excessive request rate (DDoS protection)",
                    durationInMinutes: 60);
                dbContext.BlockedIps.Add(blockedIp);
                await dbContext.SaveChangesAsync();

                _logger.LogWarning($"IP {ipAddress} blocked due to rate limiting ({requestCount} req/min).");

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. You are blocked for 1 hour.");
                return;
            }

            await dbContext.SaveChangesAsync();
        }

        await _next(context);
    }
}