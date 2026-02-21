using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;

namespace Chilla.Domain.Aggregates.CartAggregate;

public class Cart : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    
    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items.AsReadOnly();

    // اطلاعات کوپن اعمال شده روی سبد
    public Guid? CouponId { get; private set; }
    public string? CouponCode { get; private set; }

    private Cart() { }

    public Cart(Guid userId)
    {
        UserId = userId;
    }

    public void AddItem(Guid planId, decimal price)
    {
        if (_items.Any(i => i.PlanId == planId))
            throw new DomainException("این چله از قبل در سبد خرید شما وجود دارد.");

        _items.Add(new CartItem(planId, price));
    }

    public void RemoveItem(Guid planId)
    {
        var item = _items.FirstOrDefault(i => i.PlanId == planId);
        if (item != null)
        {
            _items.Remove(item);
        }

        // اگر سبد خالی شد، منطقی است که کوپن هم لغو شود
        if (!_items.Any())
        {
            RemoveCoupon();
        }
    }

    public void ApplyCoupon(Guid couponId, string couponCode)
    {
        if (!_items.Any())
            throw new DomainException("سبد خرید شما خالی است.");

        CouponId = couponId;
        CouponCode = couponCode.ToUpper();
    }

    public void RemoveCoupon()
    {
        CouponId = null;
        CouponCode = null;
    }

    public decimal GetTotalAmount() => _items.Sum(i => i.Price);
    
    public void ClearCart()
    {
        _items.Clear();
        RemoveCoupon();
    }
}