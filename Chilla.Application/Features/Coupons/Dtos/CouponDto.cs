using Chilla.Domain.Aggregates.CouponAggregate;

namespace Chilla.Application.Features.Coupons.Dtos;

public record CouponDto(
    Guid Id, 
    string Code, 
    DiscountType DiscountType, 
    decimal DiscountValue, 
    decimal? MaxDiscountAmount, 
    decimal? MinPurchaseAmount, 
    int? MaxUsageCount, 
    int CurrentUsageCount, 
    DateTime? StartDate, 
    DateTime? EndDate, 
    bool IsActive, 
    Guid? SpecificUserId,
    DateTime CreatedAt);