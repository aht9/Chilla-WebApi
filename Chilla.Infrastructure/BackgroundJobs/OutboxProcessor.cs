using System.Text.Json;
using System.Threading.Tasks.Dataflow;
using Chilla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chilla.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ActionBlock<OutboxMessage> _workerBlock;
    private readonly ILogger<OutboxProcessor> _logger;
    
    public OutboxProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // TPL Dataflow ActionBlock definition
        _workerBlock = new ActionBlock<OutboxMessage>(
            async message => await ProcessMessage(message),
            new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 4 } // پردازش همزمان ۴ پیام
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            // Fetch unprocessed messages
            var messages = await db.OutboxMessages
                .Where(m => m.ProcessedDate == null)
                .Take(50)
                .ToListAsync(stoppingToken);

            foreach (var msg in messages)
            {
                // Post to TPL Block
                _workerBlock.Post(msg);
            }

            await Task.Delay(5000, stoppingToken); // Polling interval
        }
    }

    private async Task ProcessMessage(OutboxMessage message)
    {
        try 
        {
            _logger.LogInformation("Processing Outbox Message: {Type} | ID: {Id}", message.Type, message.Id);

            if (message.Type == "UserRegisteredEvent")
            {
                // Deserialize payload
                var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(message.Content);
                
                // Real Logic Implementation:
                // فرض کنید ISmsSender تزریق شده است
                // await _smsSender.SendWelcomeSms(evt.Phone);
                
                _logger.LogInformation("Welcome SMS sent to {Phone}", evt.Phone);
            }
            
            message.ProcessedDate = DateTime.UtcNow;
            // ذخیره تغییرات در دیتابیس باید اینجا انجام شود (در اسکوپ جدید)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing outbox message {Id}", message.Id);
            message.Error = ex.Message;
            // Retry policy implementation needed here
        }
    }
}