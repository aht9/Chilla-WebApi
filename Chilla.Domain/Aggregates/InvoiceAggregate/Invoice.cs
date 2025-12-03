namespace Chilla.Domain.Aggregates.InvoiceAggregate;

public class Invoice : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    
    // اطلاعات درگاه پرداخت
    public string? GatewayName { get; private set; }
    public string? Authority { get; private set; } // توکن اولیه بانک
    public string? RefId { get; private set; }     // کد رهگیری نهایی
    
    public PaymentStatus Status { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Invoice() { }

    public Invoice(Guid userId, Guid planId, decimal amount, string? description)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");

        UserId = userId;
        PlanId = planId;
        Amount = amount;
        Description = description;
        Status = PaymentStatus.Pending;

        AddDomainEvent(new InvoiceCreatedEvent(this.Id, userId, amount));
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
        if (Status == PaymentStatus.Paid) return; // Idempotency: قبلاً پرداخت شده
        if (Status == PaymentStatus.Canceled) throw new InvalidOperationException("Cannot pay a canceled invoice.");

        Status = PaymentStatus.Paid;
        RefId = refId;
        PaidAt = DateTime.UtcNow;
        UpdateAudit();

        AddDomainEvent(new InvoicePaidEvent(this.Id, this.UserId, this.PlanId, refId));
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == PaymentStatus.Paid) return; // نمی‌توان موفق را شکست‌خورده کرد

        Status = PaymentStatus.Failed;
        Description = string.IsNullOrEmpty(Description) ? $"Failed: {reason}" : $"{Description} | Failed: {reason}";
        UpdateAudit();

        AddDomainEvent(new InvoiceFailedEvent(this.Id, this.UserId, reason));
    }
}