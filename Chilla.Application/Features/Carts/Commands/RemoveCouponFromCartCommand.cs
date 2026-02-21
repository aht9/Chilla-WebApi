using Chilla.Application.Services.Interface;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Carts.Commands;

public record RemoveCouponFromCartCommand() : IRequest<Unit>;

public class RemoveCouponFromCartCommandHandler : IRequestHandler<RemoveCouponFromCartCommand, Unit>
{
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public RemoveCouponFromCartCommandHandler(AppDbContext dbContext, IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(RemoveCouponFromCartCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var cart = await _dbContext.Carts.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

        if (cart != null && cart.CouponId.HasValue)
        {
            cart.RemoveCoupon();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return Unit.Value;
    }
}