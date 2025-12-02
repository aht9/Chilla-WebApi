using Chilla.Domain.Aggregates.SubscriptionAggregate;

namespace Chilla.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserSubscriptions
            .Include(s => s.Progress) // لود کردن جدول مهم Progress
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<UserSubscription?> GetActiveByUserIdAndPlanIdAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSubscriptions
            .Where(s => s.UserId == userId && 
                        s.PlanId == planId && 
                        s.Status == SubscriptionStatus.Active)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<List<UserSubscription>> GetByUserIdWithProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSubscriptions
            .Include(s => s.Progress)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.UserSubscriptions.AddAsync(subscription, cancellationToken);
    }

    public void Update(UserSubscription subscription)
    {
        _context.UserSubscriptions.Update(subscription);
    }
}