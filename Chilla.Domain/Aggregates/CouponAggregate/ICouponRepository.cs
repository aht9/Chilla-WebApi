namespace Chilla.Domain.Aggregates.CouponAggregate;

public interface ICouponRepository
{
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<Coupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Remove(Coupon coupon);
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}