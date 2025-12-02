using System.Text.Json;
using Chilla.Domain.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chilla.Infrastructure.Persistence.Interceptors;

public class ConvertDomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ConvertDomainEventsToOutboxMessages(eventData.Context).GetAwaiter().GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        await ConvertDomainEventsToOutboxMessages(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task ConvertDomainEventsToOutboxMessages(DbContext? context)
    {
        if (context == null) return;

        // 1. استخراج موجودیت‌هایی که ایونت دارند
        var aggregates = context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var outboxMessages = new List<OutboxMessage>();

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                outboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    OccurredOn = DateTime.UtcNow,
                    // تغییر مهم: ذخیره نام کامل تایپ شامل اسمبلی برای Deserialization دقیق
                    Type = domainEvent.GetType().AssemblyQualifiedName!, 
                    Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
                });
            }
            
            aggregate.ClearDomainEvents();
        }

        if (outboxMessages.Any())
        {
            await context.Set<OutboxMessage>().AddRangeAsync(outboxMessages);
        }
    }
}