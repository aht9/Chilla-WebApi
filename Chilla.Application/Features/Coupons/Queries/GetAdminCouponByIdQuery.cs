using Chilla.Application.Features.Coupons.Dtos;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Coupons.Queries;

public record GetAdminCouponByIdQuery(Guid Id) : IRequest<CouponDto>;

public class GetAdminCouponByIdQueryHandler : IRequestHandler<GetAdminCouponByIdQuery, CouponDto>
{
    private readonly IDapperService _dapperService;

    public GetAdminCouponByIdQueryHandler(IDapperService dapperService)
    {
        _dapperService = dapperService;
    }

    public async Task<CouponDto> Handle(GetAdminCouponByIdQuery request, CancellationToken cancellationToken)
    {
        var sql = "SELECT * FROM Coupons WHERE Id = @Id AND IsDeleted = 0";
        var coupon = await _dapperService.QuerySingleOrDefaultAsync<CouponDto>(sql, new { Id = request.Id }, cancellationToken: cancellationToken);
        
        if (coupon == null) throw new KeyNotFoundException("کد تخفیف یافت نشد.");
        return coupon;
    }
}