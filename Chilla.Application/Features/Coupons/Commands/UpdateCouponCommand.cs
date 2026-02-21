using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Coupons.Commands;

public record UpdateCouponCommand(
    Guid Id,
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinPurchaseAmount,
    int? MaxUsageCount,
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? SpecificUserId,
    bool IsActive) : IRequest<Unit>;

public class UpdateCouponCommandHandler : IRequestHandler<UpdateCouponCommand, Unit>
{
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCouponCommandHandler(AppDbContext dbContext, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(UpdateCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _dbContext.Coupons.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (coupon == null) throw new KeyNotFoundException("کد تخفیف یافت نشد.");

        // بررسی اینکه کد جدید با کد کوپن دیگری تداخل نداشته باشد
        var isCodeTaken = await _dbContext.Coupons.AnyAsync(c => c.Code == request.Code && c.Id != request.Id, cancellationToken);
        if (isCodeTaken) throw new Exception("این کد تخفیف قبلاً ثبت شده است.");

        // استفاده از متد دامین برای اعمال تغییرات
        coupon.Update(request.Code, request.DiscountType, request.DiscountValue, request.MaxDiscountAmount, 
            request.MinPurchaseAmount, request.MaxUsageCount, request.StartDate, request.EndDate, request.SpecificUserId, request.IsActive);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}