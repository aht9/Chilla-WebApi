using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chilla.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly ActionBlock<Guid> _workerBlock; // Change: Pass ID instead of Entity to avoid context issues

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // تنظیمات پردازش موازی
        _workerBlock = new ActionBlock<Guid>(
            async id => await ProcessMessage(id),
            new ExecutionDataflowBlockOptions 
            { 
                MaxDegreeOfParallelism = 4,
                SingleProducerConstrained = true
            } 
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                // فقط ID پیام‌های پردازش نشده را می‌گیریم تا در ترد جداگانه دوباره از دیتابیس بگیریم
                // این کار از مشکلات Concurrency و Disposed Context جلوگیری می‌کند
                var messageIds = await db.OutboxMessages
                    .Where(m => m.ProcessedDate == null)
                    .OrderBy(m => m.OccurredOn)
                    .Take(50)
                    .Select(m => m.Id)
                    .ToListAsync(stoppingToken);

                foreach (var id in messageIds)
                {
                    await _workerBlock.SendAsync(id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        
        _workerBlock.Complete();
        await _workerBlock.Completion;
    }

    private async Task ProcessMessage(Guid messageId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // واکشی مجدد پیام در اسکوپ جدید
        var message = await db.OutboxMessages.FindAsync(messageId);
        if (message == null || message.ProcessedDate != null) return;

        try 
        {
            _logger.LogInformation("Processing Outbox Message: {Type} | ID: {Id}", message.Type, message.Id);

            if (message.Type == "UserRegisteredEvent")
            {
                // استفاده از JsonElement برای انعطاف‌پذیری در خواندن پی‌لود (چون ممکن است ساختار DTO دقیق نباشد)
                using var doc = JsonDocument.Parse(message.Content);
                var root = doc.RootElement;
                
                var userId = Guid.Parse(root.GetProperty("UserId").GetString()!);
                var phone = root.GetProperty("Phone").GetString();

                if (!string.IsNullOrEmpty(phone))
                {
                    // 1. شبیه‌سازی ارسال SMS (در واقعیت ISmsSender صدا زده می‌شود)
                    bool isSuccess = true; 
                    string error = null;
                    
                    // await _smsSender.SendAsync(phone, "به چله خوش آمدید!");

                    // 2. ثبت لاگ دقیق در دیتابیس (NotificationLog)
                    var notifLog = new NotificationLog(
                        userId, 
                        NotificationType.Sms, 
                        "Welcome to Chilla Application", 
                        phone
                    );

                    if (isSuccess)
                        notifLog.MarkAsSent();
                    else
                        notifLog.MarkAsFailed(error ?? "Unknown error");

                    db.NotificationLogs.Add(notifLog);
                }
            }
            
            // مارک کردن پیام به عنوان پردازش شده
            message.ProcessedDate = DateTime.UtcNow;
            
            // ذخیره همه تغییرات (آپدیت Outbox + اینسرت NotificationLog) در یک تراکنش
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
            
            // ثبت خطا در خود پیام برای بررسی بعدی
            message.Error = ex.Message;
            try { await db.SaveChangesAsync(); } catch { /* Ignore save error */ }
        }
    }
}