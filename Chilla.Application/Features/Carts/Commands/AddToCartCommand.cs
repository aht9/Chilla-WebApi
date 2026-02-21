using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.CartAggregate;
using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Carts.Commands;

public record AddToCartCommand(Guid PlanId) : IRequest<Unit>;

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Unit>
{
    private readonly AppDbContext _dbContext; 
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AddToCartCommandHandler(AppDbContext dbContext, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId.Value;

        // ۱. بررسی وجود چله و استخراج قیمت آن
        var plan = await _dbContext.Set<Plan>().FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
        if (plan == null) throw new KeyNotFoundException("چله مورد نظر یافت نشد.");

        // ۲. پیدا کردن سبد خرید کاربر یا ساخت سبد جدید
        var cart = await _dbContext.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null)
        {
            cart = new Cart(userId);
            await _dbContext.Carts.AddAsync(cart, cancellationToken);
        }

        // ۳. استفاده از متد دامین برای اضافه کردن
        cart.AddItem(plan.Id, plan.Price);

        // ۴. اگر کوپنی از قبل اعمال شده بود، با اضافه شدن آیتم جدید باید دوباره اعتبارسنجی شود
        // برای سادگی و امنیت، در صورت تغییر سبد، کوپن را حذف می‌کنیم تا کاربر مجدد اعمال کند
        if (cart.CouponId.HasValue)
        {
            cart.RemoveCoupon();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}