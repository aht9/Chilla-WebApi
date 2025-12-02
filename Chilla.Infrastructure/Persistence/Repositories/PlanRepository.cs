using Chilla.Domain.Aggregates.PlanAggregate;

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
}