using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using Microsoft.EntityFrameworkCore; // در صورت نیاز

namespace Chilla.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// واکشی اشتراک به همراه تمام پیشرفت‌های روزانه (جهت ثبت پیشرفت جدید و بررسی بیزینس رول‌ها)
    /// </summary>
    public async Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.UserSubscriptions
            .Include(s => s.DailyProgresses) // بسیار مهم: این Include باعث می‌شود متد RecordTaskProgress به درستی کار کند
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    /// <summary>
    /// واکشی چله فعال کاربر بر اساس آیدی پلن
    /// </summary>
    public async Task<UserSubscription?> GetActiveByUserIdAndPlanIdAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSubscriptions
            .Where(s => s.UserId == userId && 
                        s.PlanId == planId && 
                        s.Status == SubscriptionStatus.Active)
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// واکشی تمام چله‌های یک کاربر (نکته: برای داشبورد و لیست‌های نمایشی ما از Dapper استفاده کردیم که بهینه‌تر است، 
    /// اما وجود این متد برای عملیات دامین و Command ها مفید است)
    /// </summary>
    public async Task<List<UserSubscription>> GetByUserIdWithProgressAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSubscriptions
            .Include(s => s.DailyProgresses)
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
        // در EF Core وقتی موجودیت را Track کرده باشیم، خود SaveChanges تغییرات را می‌فهمد
        // اما فراخوانی 명ریح Update برای اطمینان در برخی سناریوها مشکلی ندارد
        _context.UserSubscriptions.Update(subscription);
    }
    
    public async Task<int> CountAsync(ISpecification<UserSubscription> spec, CancellationToken cancellationToken)
    {
        var queryable = _context.UserSubscriptions.AsQueryable();
        var query = SpecificationEvaluator.GetQuery(queryable, spec);
        return await query.CountAsync(cancellationToken);
    }
}