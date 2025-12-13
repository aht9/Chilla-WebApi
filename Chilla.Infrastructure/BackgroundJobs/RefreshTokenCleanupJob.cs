using Chilla.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Chilla.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public class RefreshTokenCleanupJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(AppDbContext dbContext, ILogger<RefreshTokenCleanupJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting Refresh Token Cleanup Job...");

        try
        {
            // تعیین مرز زمانی: مثلاً توکن‌هایی که ۷ روز پیش منقضی یا باطل شده‌اند
            // (این فاصله ۷ روزه برای Audit Log و بررسی‌های امنیتی مفید است)
            var threshold = DateTime.UtcNow.AddDays(-7);

            // استفاده از ExecuteDeleteAsync برای حذف سریع و دسته‌جمعی (Bulk Delete)
            // این روش بسیار سریع‌تر از واکشی و حذف تک‌تک است و رم سرور را اشغال نمی‌کند.
            var deletedCount = await _dbContext.UserRefreshTokens
                .Where(t => t.Expires < threshold || (t.Revoked != null && t.Revoked < threshold))
                .ExecuteDeleteAsync(context.CancellationToken);

            if (deletedCount > 0)
            {
                _logger.LogInformation("Deleted {Count} old refresh tokens successfully.", deletedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up refresh tokens.");
        }
    }
}