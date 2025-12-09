using Chilla.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Chilla.Infrastructure.BackgroundJobs;

[DisallowConcurrentExecution]
public class OutboxCleanupJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<OutboxCleanupJob> _logger;

    public OutboxCleanupJob(AppDbContext dbContext, ILogger<OutboxCleanupJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting Outbox Cleanup (Batch Mode)...");

        try
        {
            var retentionDate = DateTime.UtcNow.AddDays(-3); 
            int batchSize = 2000; 
            int totalDeleted = 0;
            bool hasMore = true;

            while (hasMore)
            {
                var deletedCount = await _dbContext.OutboxMessages
                    .Where(m => m.ProcessedDate != null && m.ProcessedDate < retentionDate)
                    .OrderBy(m => m.ProcessedDate) // مرتب‌سازی برای Take ضروری است
                    .Take(batchSize)
                    .ExecuteDeleteAsync(context.CancellationToken);

                totalDeleted += deletedCount;

                if (deletedCount < batchSize)
                {
                    // اگر کمتر از ۲۰۰۰ تا پاک کرد، یعنی دیگر چیزی نمانده
                    hasMore = false;
                }
                else
                {
                    await Task.Delay(100); 
                }
            }

            if (totalDeleted > 0)
                _logger.LogInformation("Deleted {Count} old outbox messages in batches.", totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up outbox messages.");
        }
    }
}