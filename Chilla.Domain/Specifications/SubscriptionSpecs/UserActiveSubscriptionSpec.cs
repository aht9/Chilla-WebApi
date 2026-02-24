using Chilla.Domain.Aggregates.SubscriptionAggregate;

namespace Chilla.Domain.Specifications.SubscriptionSpecs;

public class UserActiveSubscriptionSpec : BaseSpecification<UserSubscription>
{
    public UserActiveSubscriptionSpec(Guid userId) 
        : base(s => s.UserId == userId && s.Status == SubscriptionStatus.Active && !s.IsDeleted)
    {
        AddInclude(s => s.DailyProgresses);
    }
}