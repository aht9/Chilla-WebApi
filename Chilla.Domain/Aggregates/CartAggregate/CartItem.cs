using Chilla.Domain.Common;

namespace Chilla.Domain.Aggregates.CartAggregate;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid PlanId { get; private set; }
    public decimal Price { get; private set; } // قیمت چله در لحظه افزوده شدن به سبد

    private CartItem() { } // برای EF Core

    public CartItem(Guid planId, decimal price)
    {
        PlanId = planId;
        Price = price;
    }
}