namespace Chilla.Application.Features.Carts.DTOs;

public record CartDto(Guid Id, List<CartItemDto> Items, decimal TotalAmount, decimal DiscountAmount, decimal PayableAmount, string? AppliedCouponCode);