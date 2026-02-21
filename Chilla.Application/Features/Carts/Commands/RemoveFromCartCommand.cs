using Chilla.Application.Services.Interface;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Carts.Commands;


public record RemoveFromCartCommand(Guid PlanId) : IRequest<Unit>;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, Unit>
{
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFromCartCommandHandler(AppDbContext dbContext, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var cart = await _dbContext.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart == null) throw new KeyNotFoundException("سبد خریدی یافت نشد.");

        cart.RemoveItem(request.PlanId);

        // لاجیک امنیتی: اگر سبد تغییر کرد، کوپن را حذف می‌کنیم تا کاربر مجبور شود مجدد آن را اعمال کند (جلوگیری از دور زدن شرط حداقل مبلغ)
        if (cart.CouponId.HasValue)
        {
            cart.RemoveCoupon();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}