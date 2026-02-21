using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Coupons.Commands;

public record CreateCouponCommand(
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MinPurchaseAmount,
    int? MaxUsageCount,
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? SpecificUserId) : IRequest<Guid>;

public class CreateCouponCommandHandler : IRequestHandler<CreateCouponCommand, Guid>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICouponRepository _couponRepository; 

    public CreateCouponCommandHandler(IUnitOfWork unitOfWork, ICouponRepository couponRepository)
    {
        _unitOfWork = unitOfWork;
        _couponRepository = couponRepository;
    }

    public async Task<Guid> Handle(CreateCouponCommand request, CancellationToken cancellationToken)
    {
        // بررسی تکراری نبودن کد با استفاده از متد ریپازیتوری
        var exists = await _couponRepository.ExistsByCodeAsync(request.Code.ToUpper(), cancellationToken);
        if (exists) throw new Exception("کد تخفیف تکراری است.");

        var coupon = new Coupon(
            request.Code, request.DiscountType, request.DiscountValue, 
            request.MaxDiscountAmount, request.MinPurchaseAmount, 
            request.MaxUsageCount, request.StartDate, request.EndDate, request.SpecificUserId);

        // اضافه کردن به دیتابیس از طریق ریپازیتوری
        await _couponRepository.AddAsync(coupon, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coupon.Id;
    }
}