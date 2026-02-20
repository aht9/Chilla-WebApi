using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Domain.Specifications.PlanSpecs;

public class ActivePlansSpec : BaseSpecification<Plan>
{
    public ActivePlansSpec(bool includeDetails = true) 
        : base(p => p.IsActive && !p.IsDeleted) 
    {
        ApplyOrderBy(p => p.Price);

        if (includeDetails)
        {
            AddInclude(p => p.Items);
        }
    }
}