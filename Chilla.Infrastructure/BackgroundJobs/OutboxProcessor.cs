using System.Threading.Tasks.Dataflow;
using Chilla.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chilla.Infrastructure.BackgroundJobs;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ActionBlock<OutboxMessage> _workerBlock;

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
        // Logic to dispatch event, send Email/SMS, or trigger SignalR
        // Example: Sending notification via SignalR
        if (message.Type == "UserRegisteredEvent")
        {
            // Send Welcome Email logic...
            // Send SignalR Notification logic...
        }
        
        // Note: You need a scope here to update the DB that message is processed
    }
}