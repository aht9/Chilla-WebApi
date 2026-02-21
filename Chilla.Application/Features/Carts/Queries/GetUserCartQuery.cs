using Chilla.Application.Features.Carts.DTOs;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Carts.Queries;

public record GetUserCartQuery : IRequest<CartDto>;

public class GetUserCartQueryHandler : IRequestHandler<GetUserCartQuery, CartDto>
{
    private readonly IDapperService _dapperService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICouponRepository _couponRepository;

    public GetUserCartQueryHandler(IDapperService dapperService, ICurrentUserService currentUserService,
        ICouponRepository couponRepository)
    {
        _dapperService = dapperService;
        _currentUserService = currentUserService;
        _couponRepository = couponRepository;
    }

    public async Task<CartDto> Handle(GetUserCartQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        // واکشی سبد و آیتم‌ها با استفاده از QueryMultipleAsync در Dapper برای پرفورمنس بالا
        var sql = @"
            SELECT Id, CouponCode, CouponId FROM Carts WHERE UserId = @UserId AND IsDeleted = 0;
            
            SELECT ci.Id, ci.PlanId, ci.Price, p.Title as PlanTitle 
            FROM CartItems ci
            INNER JOIN Carts c ON ci.CartId = c.Id
            INNER JOIN Plans p ON ci.PlanId = p.Id
            WHERE c.UserId = @UserId AND ci.IsDeleted = 0 AND c.IsDeleted = 0;";

        using var grid =
            await _dapperService.QueryMultipleAsync(sql, new { UserId = userId }, cancellationToken: cancellationToken);

        var cartHeader = await grid.ReadSingleOrDefaultAsync();
        if (cartHeader == null)
            return new CartDto(Guid.Empty, new List<CartItemDto>(), 0, 0, 0, null);

        var items = (await grid.ReadAsync<CartItemDto>()).ToList();
        var totalAmount = items.Sum(i => i.Price);
        decimal discountAmount = 0;

        // اگر کوپنی ثبت شده بود، به صورت درلحظه مبلغ تخفیف را با لاجیک دامین محاسبه می‌کنیم
        if (cartHeader.CouponId != null)
        {
            var coupon = await _couponRepository.GetByIdAsync((Guid)cartHeader.CouponId, cancellationToken);
            if (coupon != null && coupon.IsActive)
            {
                try
                {
                    coupon.ValidateForUse(userId, totalAmount);
                    discountAmount = coupon.CalculateDiscountAmount(totalAmount);
                }
                catch
                {
                    // اگر کوپن منقضی شده باشد تخفیف صفر در نظر گرفته می‌شود
                    discountAmount = 0;
                }
            }
        }

        return new CartDto(
            Id: cartHeader.Id,
            Items: items,
            TotalAmount: totalAmount,
            DiscountAmount: discountAmount,
            PayableAmount: totalAmount - discountAmount,
            AppliedCouponCode: cartHeader.CouponCode
        );
    }
}