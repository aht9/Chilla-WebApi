using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Exceptions;
using MediatR;

namespace Chilla.Application.Features.Coupons.Queries;

public record ValidateCouponQuery(string Code, decimal CartTotalAmount) : IRequest<CouponValidationResultDto>;

public record CouponValidationResultDto(bool IsValid, string Message, decimal PayableAmount, decimal DiscountAmount, Guid? CouponId);

public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, CouponValidationResultDto>
{
    private readonly ICouponRepository _couponRepository;
    private readonly ICurrentUserService _currentUserService;

    public ValidateCouponQueryHandler(ICouponRepository couponRepository, ICurrentUserService currentUserService)
    {
        _couponRepository = couponRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CouponValidationResultDto> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
    {
        // دریافت اطلاعات کوپن از طریق ریپازیتوری (بدون درگیری با EF Core در این لایه)
        var coupon = await _couponRepository.GetByCodeAsync(request.Code, cancellationToken);

        if (coupon == null)
            return new CouponValidationResultDto(false, "کد تخفیف نامعتبر است.", request.CartTotalAmount, 0, null);

        try
        {
            // دریافت و تبدیل آیدی کاربر 
            var userId = _currentUserService.UserId.Value;
            
            // فراخوانی لاجیک دامین برای اعتبارسنجی (Business Rules)
            coupon.ValidateForUse(userId, request.CartTotalAmount);

            // محاسبه مبلغ نهایی تخفیف و مبلغ قابل پرداخت
            var discountAmount = coupon.CalculateDiscountAmount(request.CartTotalAmount);
            var payableAmount = request.CartTotalAmount - discountAmount;

            return new CouponValidationResultDto(true, "کد تخفیف با موفقیت اعمال شد.", payableAmount, discountAmount, coupon.Id);
        }
        catch (DomainException ex)
        {
            // مدیریت خطاهای مربوط به قوانین دامین (مانند انقضا، ظرفیت و ...)
            return new CouponValidationResultDto(false, ex.Message, request.CartTotalAmount, 0, null);
        }
    }
}