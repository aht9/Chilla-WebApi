using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chilla.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly ActionBlock<Guid> _workerBlock;

    public OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // تنظیمات TPL Dataflow برای پردازش موازی و کنترل فشار
        _workerBlock = new ActionBlock<Guid>(
            async id => await ProcessMessage(id),
            new ExecutionDataflowBlockOptions 
            { 
                MaxDegreeOfParallelism = 4, 
                SingleProducerConstrained = true,
                BoundedCapacity = 100 // جلوگیری از پر شدن حافظه
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
                
                // فقط ID پیام‌های پردازش نشده را می‌خوانیم
                var messageIds = await db.OutboxMessages
                    .Where(m => m.ProcessedDate == null && m.Error == null) // خطادارها را فعلا نادیده می‌گیریم تا دستی بررسی شوند یا مکانیزم Retry جدا داشته باشند
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

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
        
        _workerBlock.Complete();
        await _workerBlock.Completion;
    }

    private async Task ProcessMessage(Guid messageId)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>(); // MediatR Publisher

        var message = await db.OutboxMessages.FindAsync(messageId);
        if (message == null || message.ProcessedDate != null) return;

        try 
        {
            // 1. پیدا کردن Type واقعی ایونت از روی رشته ذخیره شده
            var eventType = Type.GetType(message.Type);
            if (eventType == null)
            {
                throw new Exception($"Type '{message.Type}' not found. Ensure assembly is loaded.");
            }

            // 2. Deserialize کردن محتوا به آبجکت واقعی
            var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
            if (domainEvent == null)
            {
                throw new Exception("Deserialization returned null.");
            }

            // 3. انتشار ایونت در سطح اپلیکیشن (اتصال به هندلرها)
            // این خط باعث می‌شود تمام EventHandler های مربوطه (مثل ارسال ایمیل، SMS و ...) اجرا شوند
            await publisher.Publish(domainEvent);

            // 4. مارک کردن به عنوان انجام شده
            message.ProcessedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            
            _logger.LogInformation("Successfully processed event {Type} ({Id})", message.Type, message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {Id}", messageId);
            message.Error = ex.ToString(); // ذخیره کامل استک برای دیباگ
            message.ProcessedDate = null; // نال می‌گذاریم اما چون ارور دارد در کوئری بعدی نمی‌آید (طبق شرط Where بالا)
            await db.SaveChangesAsync();
        }
    }
}