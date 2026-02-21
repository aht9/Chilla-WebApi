using Chilla.Domain.Aggregates.CouponAggregate;

namespace Chilla.Infrastructure.Persistence.Repositories;

public class CouponRepository : ICouponRepository
{
    private readonly AppDbContext _dbContext;

    public CouponRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Coupons.AnyAsync(c => c.Code == code, cancellationToken);
    }

    public async Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await _dbContext.Coupons.AddAsync(coupon, cancellationToken);
    }

    public async Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Coupons.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public void Remove(Coupon coupon)
    {
        _dbContext.Coupons.Remove(coupon);
    }
    
    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        // استفاده از ToUpper برای اطمینان از تطابق کدها
        return await _dbContext.Coupons.FirstOrDefaultAsync(c => c.Code == code.ToUpper(), cancellationToken);
    }
}