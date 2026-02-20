using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Domain.Specifications.PlanSpecs;

public class PlanByIdWithItemsSpec : BaseSpecification<Plan>
{
    public PlanByIdWithItemsSpec(Guid planId) 
        : base(p => p.Id == planId)
    {
        AddInclude(p => p.Items);
    }
}