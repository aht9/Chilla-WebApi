namespace Chilla.Application.Features.Carts.DTOs;

public record CartItemDto(Guid Id, Guid PlanId, string PlanTitle, decimal Price);