using Chilla.Domain.Aggregates.SubscriptionAggregate;

namespace Chilla.Domain.Specifications.SubscriptionSpecs;

public class SubscriptionsByPlanIdSpec : BaseSpecification<UserSubscription>
{
    public SubscriptionsByPlanIdSpec(Guid planId)
        : base(s => s.PlanId == planId)
    {
        // ما فقط نیاز داریم بدانیم آیا رکوردی وجود دارد یا خیر
        // بنابراین نیازی به Include کردن نیست
    }
}