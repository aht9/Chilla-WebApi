using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Carts.Commands;

public record ApplyCouponToCartCommand(string CouponCode) : IRequest<Unit>;

public class ApplyCouponToCartCommandHandler : IRequestHandler<ApplyCouponToCartCommand, Unit>
{
    private readonly AppDbContext _dbContext;
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public ApplyCouponToCartCommandHandler(AppDbContext dbContext, ICouponRepository couponRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(ApplyCouponToCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var cart = await _dbContext.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null || !cart.Items.Any()) 
            throw new Exception("سبد خرید شما خالی است.");

        var coupon = await _couponRepository.GetByCodeAsync(request.CouponCode, cancellationToken);
        if (coupon == null) throw new KeyNotFoundException("کد تخفیف نامعتبر است.");

        // اعتبارسنجی دقیق دامین (زمان، ظرفیت، تعلق به کاربر و حداقل مبلغ)
        coupon.ValidateForUse(userId, cart.GetTotalAmount());

        cart.ApplyCoupon(coupon.Id, coupon.Code);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}