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

        // 1. پیدا کردن تمام موجودیت‌هایی که DomainEvent دارند
        var outboxMessages = context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .SelectMany(aggregate =>
            {
                var domainEvents = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents(); // پاک کردن ایونت‌ها بعد از برداشتن
                return domainEvents;
            })
            .Select(domainEvent => new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOn = DateTime.UtcNow,
                Type = domainEvent.GetType().Name, // ذخیره نام کلاس ایونت برای Deserialize
                Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()) // سریالایز کردن کامل
            })
            .ToList();

        // 2. افزودن به DbSet مربوطه
        if (outboxMessages.Any())
        {
            await context.Set<OutboxMessage>().AddRangeAsync(outboxMessages);
        }
    }
}