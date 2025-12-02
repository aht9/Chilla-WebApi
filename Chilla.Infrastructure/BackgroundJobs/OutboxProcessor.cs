using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Chilla.Infrastructure.BackgroundJobs;

// این اتریبیوت تضمین می‌کند که اگر اجرای جاب قبلی هنوز تمام نشده، جاب جدید شروع نشود
[DisallowConcurrentExecution]
public class OutboxProcessJob : IJob
{
    private readonly AppDbContext _dbContext;
    private readonly IPublisher _publisher;
    private readonly ILogger<OutboxProcessJob> _logger;

    // در Quartz.NET (با کانفیگ DI)، سرویس‌های Scoped مثل DbContext به صورت خودکار مدیریت می‌شوند
    public OutboxProcessJob(AppDbContext dbContext, IPublisher publisher, ILogger<OutboxProcessJob> logger)
    {
        _dbContext = dbContext;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            // 1. دریافت پیام‌های پردازش نشده (Batch Processing)
            // تعداد 20 عدد برای جلوگیری از لود ناگهانی روی سیستم
            var messages = await _dbContext.OutboxMessages
                .Where(m => m.ProcessedDate == null && m.Error == null)
                .OrderBy(m => m.OccurredOn)
                .Take(20)
                .ToListAsync(context.CancellationToken);

            if (!messages.Any()) return;

            foreach (var message in messages)
            {
                try
                {
                    // 2. Deserialize و انتشار
                    var eventType = Type.GetType(message.Type);
                    if (eventType == null)
                    {
                        throw new Exception($"Type '{message.Type}' not found.");
                    }

                    var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
                    if (domainEvent != null)
                    {
                        // انتشار به لایه Application
                        await _publisher.Publish(domainEvent, context.CancellationToken);
                    }

                    // 3. آپدیت وضعیت موفقیت
                    message.ProcessedDate = DateTime.UtcNow;
                    _logger.LogInformation("Processed Outbox Message: {Id}", message.Id);
                }
                catch (Exception ex)
                {
                    // ثبت خطا بدون متوقف کردن کل پروسه
                    _logger.LogError(ex, "Failed to process message {Id}", message.Id);
                    message.Error = ex.ToString();
                    // می‌توان استراتژی Retry را اینجا پیاده کرد (مثلاً افزایش RetryCount)
                }
            }

            // 4. ذخیره تغییرات (Batch Update)
            await _dbContext.SaveChangesAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in OutboxProcessJob");
        }
    }
}