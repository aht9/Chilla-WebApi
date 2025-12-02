using MediatR;

namespace Chilla.Domain.Aggregates.UserAggregate.Events;

public record UserRegisteredEvent(User User) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserLockedOutEvent(Guid UserId, DateTimeOffset LockoutEnd) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record UserProfileUpdatedEvent(Guid UserId) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}