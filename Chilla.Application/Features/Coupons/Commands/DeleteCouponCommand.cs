using Chilla.Domain.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Coupons.Commands;

public record DeleteCouponCommand(Guid Id) : IRequest<Unit>;

public class DeleteCouponCommandHandler : IRequestHandler<DeleteCouponCommand, Unit>
{
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCouponCommandHandler(AppDbContext dbContext, IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteCouponCommand request, CancellationToken cancellationToken)
    {
        var coupon = await _dbContext.Coupons.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (coupon == null) throw new KeyNotFoundException("کد تخفیف یافت نشد.");

        // اگر سیستم شما AuditableEntityInterceptor دارد و فیلد IsDeleted را خودش آپدیت می‌کند 
        // همین متد Remove کافیست (تبدیل به Soft Delete می‌شود).
        _dbContext.Coupons.Remove(coupon);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}