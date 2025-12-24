namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public interface ISubscriptionRepository
{
    Task<UserSubscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    // گرفتن اشتراک فعال کاربر برای یک پلن خاص (جلوگیری از ثبت تکراری)
    Task<UserSubscription?> GetActiveByUserIdAndPlanIdAsync(Guid userId, Guid planId, CancellationToken cancellationToken = default);
    
    // گرفتن تمام اشتراک‌های یک کاربر همراه با جزئیات پیشرفت (برای محاسبه درصد پیشرفت)
    Task<List<UserSubscription>> GetByUserIdWithProgressAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task AddAsync(UserSubscription subscription, CancellationToken cancellationToken = default);
    void Update(UserSubscription subscription);
    
    Task<int> CountAsync(ISpecification<UserSubscription> spec, CancellationToken cancellationToken);

}