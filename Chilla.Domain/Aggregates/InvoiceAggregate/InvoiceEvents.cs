namespace Chilla.Domain.Aggregates.InvoiceAggregate;

public record InvoiceCreatedEvent(Guid InvoiceId, Guid UserId, decimal Amount) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record InvoicePaidEvent(Guid InvoiceId, Guid UserId, Guid PlanId, string RefId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record InvoiceFailedEvent(Guid InvoiceId, Guid UserId, string Reason) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}