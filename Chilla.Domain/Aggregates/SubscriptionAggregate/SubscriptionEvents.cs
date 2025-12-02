namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public record SubscriptionCreatedEvent(Guid SubscriptionId, Guid UserId, Guid PlanId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record DailyProgressLoggedEvent(Guid SubscriptionId, Guid UserId, Guid PlanTemplateItemId, int Value, bool IsCompleted) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SubscriptionCompletedEvent(Guid SubscriptionId, Guid UserId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record SubscriptionFailedEvent(Guid SubscriptionId, Guid UserId, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}