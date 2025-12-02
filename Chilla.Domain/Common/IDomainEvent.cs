namespace Chilla.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}