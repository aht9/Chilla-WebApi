using MediatR;

namespace Chilla.Domain.Common;

public interface IDomainEvent: INotification
{
    DateTime OccurredOn { get; }
}