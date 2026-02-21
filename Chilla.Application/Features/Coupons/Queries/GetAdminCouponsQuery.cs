using Chilla.Application.Features.Coupons.Dtos;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Coupons.Queries;

public record GetAdminCouponsQuery : IRequest<IReadOnlyList<CouponDto>>;

public class GetAdminCouponsQueryHandler : IRequestHandler<GetAdminCouponsQuery, IReadOnlyList<CouponDto>>
{
    private readonly IDapperService _dapperService;

    public GetAdminCouponsQueryHandler(IDapperService dapperService)
    {
        _dapperService = dapperService;
    }

    public async Task<IReadOnlyList<CouponDto>> Handle(GetAdminCouponsQuery request, CancellationToken cancellationToken)
    {
        // در صورت وجود سافت دلیت از شرط IsDeleted = 0 استفاده کنید
        var sql = @"
            SELECT 
                Id, Code, DiscountType, DiscountValue, MaxDiscountAmount, 
                MinPurchaseAmount, MaxUsageCount, CurrentUsageCount, 
                StartDate, EndDate, IsActive, SpecificUserId, CreatedAt 
            FROM Coupons 
            WHERE IsDeleted = 0 
            ORDER BY CreatedAt DESC";
        return await _dapperService.QueryAsync<CouponDto>(sql, cancellationToken: cancellationToken);
    }
}