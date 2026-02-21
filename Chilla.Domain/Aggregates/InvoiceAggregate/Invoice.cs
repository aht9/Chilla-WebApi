namespace Chilla.Domain.Aggregates.InvoiceAggregate;

public class Invoice : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid? PlanId { get; private set; } // نال‌بل شد تا برای سبد خرید (چندین چله) ارور ندهد
    
    // فیلدهای جدید برای پشتیبانی از سبد خرید و کوپن
    public decimal TotalAmount { get; private set; } 
    public decimal DiscountAmount { get; private set; }
    public string? CouponCode { get; private set; }
    
    public decimal Amount { get; private set; } // مبلغ نهایی پرداختی
    public string? Description { get; private set; }
    
    // اطلاعات درگاه پرداخت
    public string? GatewayName { get; private set; }
    public string? Authority { get; private set; } 
    public string? RefId { get; private set; }     
    
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Invoice() { Id = Guid.NewGuid(); }

    // سازنده قدیمی (برای خریدهای تکی مستقیم)
    public Invoice(Guid userId, Guid planId, decimal amount, string? description)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = planId;
        TotalAmount = amount;
        Amount = amount;
        Description = description;
        Status = PaymentStatus.Pending;

        AddDomainEvent(new InvoiceCreatedEvent(this.Id, userId, amount));
    }

    // سازنده جدید (مخصوص سبد خرید و اعمال کوپن)
    public Invoice(Guid userId, decimal totalAmount, decimal discountAmount, decimal payableAmount, string? couponCode, string? description = "پرداخت سبد خرید")
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (payableAmount < 0) throw new ArgumentException("PayableAmount cannot be negative");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = null; // چون سبد خرید است، به یک پلن خاص محدود نمی‌شود
        TotalAmount = totalAmount;
        DiscountAmount = discountAmount;
        Amount = payableAmount;
        CouponCode = couponCode;
        Description = description;
        Status = PaymentStatus.Pending;

        AddDomainEvent(new InvoiceCreatedEvent(this.Id, userId, payableAmount));
    }

    public void SetGatewayAuthority(string gatewayName, string authority)
    {
        if (Status != PaymentStatus.Pending) 
            throw new InvalidOperationException("Cannot set authority for non-pending invoice.");
            
        GatewayName = gatewayName;
        Authority = authority;
        UpdateAudit();
    }

    public void MarkAsPaid(string refId)
    {
        if (Status == PaymentStatus.Paid) return; 
        if (Status == PaymentStatus.Canceled) throw new InvalidOperationException("Cannot pay a canceled invoice.");

        Status = PaymentStatus.Paid;
        RefId = refId;
        PaidAt = DateTime.UtcNow;
        UpdateAudit();

        // نکته: اگر InvoicePaidEvent فقط Guid می‌گیرد، از ?? Guid.Empty استفاده می‌کنیم
        AddDomainEvent(new InvoicePaidEvent(this.Id, this.UserId, this.PlanId ?? Guid.Empty, refId));
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == PaymentStatus.Paid) return; 

        Status = PaymentStatus.Failed;
        Description = string.IsNullOrEmpty(Description) ? $"Failed: {reason}" : $"{Description} | Failed: {reason}";
        UpdateAudit();

        AddDomainEvent(new InvoiceFailedEvent(this.Id, this.UserId, reason));
    }
}