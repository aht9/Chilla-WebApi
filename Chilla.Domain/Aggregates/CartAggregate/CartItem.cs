using Chilla.Domain.Common;

namespace Chilla.Domain.Aggregates.CartAggregate;

public class CartItem : BaseEntity
{
    public Guid CartId { get; private set; }
    public Guid PlanId { get; private set; }

    public decimal Price { get; private set; } // قیمت چله در لحظه افزوده شدن به سبد

    // --- : ذخیره تنظیمات شخصی‌سازی شده کاربر برای این چله ---
    public string? PreferencesJson { get; private set; }

    private CartItem()
    {
    } // برای EF Core

    public CartItem(Guid planId, decimal price, string? preferencesJson)
    {
        PlanId = planId;
        Price = price;
        PreferencesJson = preferencesJson;
    }
}