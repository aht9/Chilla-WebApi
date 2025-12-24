using Chilla.Domain.Specifications.PlanSpecs;

namespace Chilla.Domain.Aggregates.PlanAggregate;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    // متد اختصاصی برای گرفتن پلن‌های فعال همراه با آیتم‌هایشان (Eager Loading)
    Task<List<Plan>> GetAllActivePlansAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
    void Update(Plan plan);
    void Delete(Plan plan);
    
    Task<List<Plan>> ListAsync(ISpecification<Plan> spec, CancellationToken cancellationToken);

    Task<Plan> FirstOrDefaultAsync(ISpecification<Plan> spec, CancellationToken cancellationToken);
}