using System.Text.Json;
using Chilla.Application.Features.Subscriptions.Commands;
using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CartAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Carts.Commands;

public record AddToCartCommand(Guid PlanId, List<UserNotificationPreferenceDto>? UserPreferences) : IRequest<Unit>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Unit>
{
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddToCartCommandHandler(AppDbContext dbContext, IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();

        // ۱. واکشی پلن به همراه تسک‌هایش (برای محاسبه قیمت)
        var plan = await _dbContext.Set<Plan>().Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);

        if (plan == null || !plan.IsActive)
            throw new KeyNotFoundException("چله مورد نظر یافت نشد یا غیرفعال است.");

        // ۲. محاسبه قیمت نهایی (با احتساب هزینه‌های مازاد SMS و تماس)
        decimal finalPrice = CalculateTotalPlanPrice(plan, request.UserPreferences);

        string? preferencesJson = request.UserPreferences != null && request.UserPreferences.Any()
            ? JsonSerializer.Serialize(request.UserPreferences)
            : null;

        // ۳. مدیریت سبد خرید
        var cart = await _dbContext.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            cart = new Cart(userId);
            await _dbContext.Carts.AddAsync(cart, cancellationToken);
        }

        // افزودن آیتم با قیمت داینامیک و تنظیمات
        cart.AddItem(plan.Id, finalPrice, preferencesJson);

        if (cart.CouponId.HasValue)
        {
            cart.RemoveCoupon(); // در صورت تغییر سبد، کوپن برای جلوگیری از تقلب حذف می‌شود
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    // متدهای CalculateTotalPlanPrice و CalculateTotalTaskInstances دقیقاً همان‌هایی هستند 
    // که در کد PurchasePlanCommandHandler شما وجود داشتند (آنها را اینجا کپی کنید).
    private decimal CalculateTotalPlanPrice(Plan plan, List<UserNotificationPreferenceDto>? userPreferences)
    {
        /* ... کدهای قبلی شما ... */
        return plan.Price;
    }
}