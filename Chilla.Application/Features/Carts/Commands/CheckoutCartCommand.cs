using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Aggregates.InvoiceAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Aggregates.SubscriptionAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Carts.Commands;

// DTO بازگشتی جدید برای مدیریت وضعیت پرداخت
public record CheckoutResultDto(Guid InvoiceId, bool RequiresPayment, decimal PayableAmount, string Message);

public record CheckoutCartCommand() : IRequest<CheckoutResultDto>;

public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, CheckoutResultDto>
{
    private readonly AppDbContext _dbContext;
    private readonly ICouponRepository _couponRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutCartCommandHandler(AppDbContext dbContext, ICouponRepository couponRepository,
        IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CheckoutResultDto> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        var cart = await _dbContext.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null || !cart.Items.Any())
            throw new Exception("سبد خرید شما خالی است.");

        var totalAmount = cart.GetTotalAmount();
        decimal discountAmount = 0;
        Coupon? coupon = null;

        if (cart.CouponId.HasValue)
        {
            coupon = await _couponRepository.GetByIdAsync(cart.CouponId.Value, cancellationToken);
            if (coupon != null)
            {
                coupon.ValidateForUse(userId, totalAmount);
                discountAmount = coupon.CalculateDiscountAmount(totalAmount);
            }
        }

        var payableAmount = totalAmount - discountAmount;
        bool requiresPayment = payableAmount > 0;

        var invoice = new Invoice(userId, totalAmount, discountAmount, payableAmount, coupon?.Code, "ثبت سفارش چله");
        await _dbContext.Set<Invoice>().AddAsync(invoice, cancellationToken);

        // واکشی پلن‌ها برای دسترسی به طول دوره (DurationInDays)
        var planIds = cart.Items.Select(i => i.PlanId).ToList();
        var plans = await _dbContext.Set<Plan>().Where(p => planIds.Contains(p.Id)).ToListAsync(cancellationToken);

        // ساخت اشتراک‌ها با انتقال PreferencesJson
        foreach (var item in cart.Items)
        {
            var planDuration = plans.FirstOrDefault(p => p.Id == item.PlanId)?.DurationInDays ?? 40;

            var subscription = new UserSubscription(
                userId: userId,
                planId: item.PlanId,
                invoiceId: invoice.Id,
                requiresPayment: requiresPayment,
                durationInDays: planDuration,
                notificationPreferencesJson: item.PreferencesJson // انتقال تنظیمات از سبد به اشتراک
            );

            await _dbContext.Set<UserSubscription>().AddAsync(subscription, cancellationToken);
        }

        cart.ClearCart();

        if (!requiresPayment)
        {
            invoice.MarkAsPaid("Paid-via-100%-Coupon");
            if (coupon != null) coupon.IncrementUsage();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CheckoutResultDto(invoice.Id, false, 0, "سفارش شما با موفقیت ثبت شد و چله‌ها فعال شدند.");
        }
        else
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CheckoutResultDto(invoice.Id, true, payableAmount,
                "سفارش ثبت شد. جهت فعال‌سازی باید پرداخت انجام شود.");
        }
    }
}