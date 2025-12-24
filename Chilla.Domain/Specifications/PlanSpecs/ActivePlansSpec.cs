using Chilla.Domain.Aggregates.PlanAggregate;

namespace Chilla.Domain.Specifications.PlanSpecs;

public class ActivePlansSpec : BaseSpecification<Plan>
{
    /*public ActivePlansSpec() : base(p => p.IsActive && !p.IsDeleted)
    {
        AddInclude(p => p.Items); // Eager load template items
    }*/
    
    public ActivePlansSpec() : base(p => p.IsActive)
    {
        OrderBy(p => p.Price);
    }
}