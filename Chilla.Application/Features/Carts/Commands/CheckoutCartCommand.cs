using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CouponAggregate;
using Chilla.Domain.Aggregates.InvoiceAggregate;
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

    public CheckoutCartCommandHandler(AppDbContext dbContext, ICouponRepository couponRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _couponRepository = couponRepository;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CheckoutResultDto> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var cart = await _dbContext.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null || !cart.Items.Any()) throw new Exception("سبد خرید شما خالی است.");

        var totalAmount = cart.GetTotalAmount();
        decimal discountAmount = 0;
        Coupon? coupon = null;

        // ۱. بررسی کوپن
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

        // ۲. ساخت فاکتور (در این مرحله همیشه با وضعیت Pending ساخته می‌شود)
        var invoice = new Invoice(userId, totalAmount, discountAmount, payableAmount, coupon?.Code, "ثبت سفارش سبد خرید");
        await _dbContext.Set<Invoice>().AddAsync(invoice, cancellationToken);

        // ۳. ایجاد اشتراک‌ها (با توجه به requiresPayment وضعیت Active یا PendingPayment می‌گیرند)
        foreach (var item in cart.Items)
        {
            var subscription = new UserSubscription(userId, item.PlanId, invoice.Id, requiresPayment);
            await _dbContext.Set<UserSubscription>().AddAsync(subscription, cancellationToken);
        }

        // ۴. خالی کردن سبد خرید (چون سفارش به فاکتور منتقل شد)
        cart.ClearCart();

        // ۵. لاجیک نهایی‌سازی بر اساس نیاز به پرداخت
        if (!requiresPayment)
        {
            // اگر رایگان شد: فاکتور پرداخت‌شده مارک می‌شود و ظرفیت کوپن کم می‌شود
            invoice.MarkAsPaid("Paid-via-100%-Coupon");
            if (coupon != null) coupon.IncrementUsage();
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new CheckoutResultDto(invoice.Id, false, 0, "سفارش شما با موفقیت ثبت شد و چله‌ها فعال شدند.");
        }
        else
        {
            // اگر نیاز به پرداخت دارد: فقط فاکتور و اشتراک‌های معلق را ذخیره می‌کنیم
            // ظرفیت کوپن را الان کم نمی‌کنیم؛ باید در زمان بازگشت موفق از بانک کم شود.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // در فاز فعلی که درگاه نداریم، می‌توانید یک خطای موقت اینجا پرتاب کنید، 
            // اما برگرداندن نتیجه زیر بسیار اصولی‌تر است. اینطوری فرانت‌اند می‌فهمد که باید پول بدهد.
            return new CheckoutResultDto(invoice.Id, true, payableAmount, "سفارش ثبت شد. جهت فعال‌سازی باید پرداخت انجام شود (درگاه در آینده اضافه خواهد شد).");
        }
    }
}