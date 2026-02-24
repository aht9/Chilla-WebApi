using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;

namespace Chilla.Infrastructure.Persistence.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly AppDbContext _context;

    public PlanRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Include(p => p.Items) // همیشه آیتم‌ها را با پلن می‌خواهیم
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<Plan>> GetAllActivePlansAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Include(p => p.Items)
            .Where(p => p.IsActive)
            .AsNoTracking() // برای لیست‌ها جهت افزایش سرعت Read-Only
            .ToListAsync(cancellationToken);
    }
    
    public async Task<List<Plan>> GetPlansByIdsAsync(IEnumerable<Guid> planIds, CancellationToken cancellationToken = default)
    {
        return await _context.Plans
            .Include(p => p.Items) // اگر در داشبورد به تسک‌ها هم نیاز دارید این خط بماند، در غیر این صورت می‌توانید حذفش کنید
            .Where(p => planIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await _context.Plans.AddAsync(plan, cancellationToken);
    }

    public void Update(Plan plan)
    {
        _context.Plans.Update(plan);
    }

    public void Delete(Plan plan)
    {
        _context.Plans.Update(plan);
    }
    
    public async Task<List<Plan>> ListAsync(ISpecification<Plan> spec, CancellationToken cancellationToken)
    {
        var queryable = _context.Plans.AsQueryable();
        // استفاده از SpecificationEvaluator که در پروژه دارید
        var query = SpecificationEvaluator.GetQuery(queryable, spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Plan> FirstOrDefaultAsync(ISpecification<Plan> spec, CancellationToken cancellationToken)
    {
        var queryable = _context.Plans.AsQueryable();
        var query = SpecificationEvaluator.GetQuery(queryable, spec);
        return await query.FirstOrDefaultAsync(cancellationToken);

    }
}