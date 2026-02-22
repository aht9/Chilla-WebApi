using Chilla.Domain.Exceptions;

namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public enum SubscriptionStatus
{
    Active,
    Completed,
    Failed,
    Canceled,
    PendingPayment
}

public class UserSubscription : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public string? NotificationPreferencesJson { get; private set; }

    private readonly List<DailyProgress> _progress = new();
    public IReadOnlyCollection<DailyProgress> Progress => _progress.AsReadOnly();

    private UserSubscription()
    {
        Id = Guid.NewGuid();
    }

    // سازنده آپدیت شده برای پذیرش آیدی فاکتور
    public UserSubscription(Guid userId, Guid planId, Guid? invoiceId, bool requiresPayment)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = planId;
        InvoiceId = invoiceId;
        StartDate = DateTime.UtcNow;

        // اگر نیاز به پرداخت دارد، وضعیت معلق می‌گیرد، در غیر اینصورت فعال می‌شود
        Status = requiresPayment ? SubscriptionStatus.PendingPayment : SubscriptionStatus.Active;
    }

    public UserSubscription(Guid userId, Guid planId, DateTime startDate, DateTime endDate, bool requiresPayment,
        string? notificationPreferencesJson)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");
        if (endDate < startDate) throw new ArgumentException("EndDate cannot be before StartDate");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = planId;
        StartDate = startDate;
        EndDate = endDate;
        NotificationPreferencesJson = notificationPreferencesJson;

        Status = requiresPayment ? SubscriptionStatus.PendingPayment : SubscriptionStatus.Active;
    }

    public void Activate()
    {
        if (Status == SubscriptionStatus.PendingPayment)
        {
            Status = SubscriptionStatus.Active;
            UpdateAudit();
        }
    }

    public void MarkTaskAsComplete(Guid planTemplateItemId, int valueEntered = 0, bool requiresUnbrokenChain = false)
    {
        if (Status != SubscriptionStatus.Active)
            throw new DomainException("Cannot update progress on an inactive or failed subscription.");

        var today = DateTime.UtcNow.Date;

        // --- منطق بررسی زنجیره پیوسته (Unbroken Chain) ---
        if (requiresUnbrokenChain)
        {
            var yesterday = today.AddDays(-1);

            // فقط در صورتی چک می‌کنیم که اشتراک کاربر حداقل از دیروز شروع شده باشد
            if (StartDate.Date <= yesterday)
            {
                // پیدا کردن پیشرفت روز قبل برای همین تسک
                var yesterdayProgress = _progress.SingleOrDefault(p =>
                    p.PlanTemplateItemId == planTemplateItemId && p.ScheduledDate.Date == yesterday);

                // اگر دیروز این تسک انجام نشده است
                if (yesterdayProgress == null || !yesterdayProgress.IsCompleted)
                {
                    // چله را فیلد می‌کنیم
                    FailSubscription();
                    throw new DomainException(
                        "شما این تسک را در روز قبل انجام نداده‌اید. زنجیره این چله شکسته شده است و متأسفانه نیازمند شروع مجدد هستید.");
                }
            }
        }

        var existing = _progress.SingleOrDefault(p =>
            p.PlanTemplateItemId == planTemplateItemId && p.ScheduledDate.Date == today);

        if (existing != null)
        {
            existing.UpdateValue(valueEntered, false, null);
        }
        else
        {
            _progress.Add(new DailyProgress(planTemplateItemId, today, valueEntered));
        }

        UpdateAudit();
    }

    public void MarkTaskAsCompleteWithCommitment(Guid planTemplateItemId, int valueEntered, string commitmentReason)
    {
        if (Status != SubscriptionStatus.Active)
            throw new InvalidOperationException("Cannot update progress on an inactive subscription.");

        var today = DateTime.UtcNow.Date;

        var existing = _progress.SingleOrDefault(p =>
            p.PlanTemplateItemId == planTemplateItemId && p.ScheduledDate.Date == today);

        if (existing != null)
        {
            existing.UpdateValue(valueEntered, true, commitmentReason);
        }
        else
        {
            _progress.Add(new DailyProgress(planTemplateItemId, today, valueEntered, true, commitmentReason));
        }

        UpdateAudit();
    }

    public void CancelSubscription()
    {
        if (Status == SubscriptionStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed subscription.");

        Status = SubscriptionStatus.Canceled;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }

    public void CompleteSubscription()
    {
        if (Status != SubscriptionStatus.Active) return;

        Status = SubscriptionStatus.Completed;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }

    public void FailSubscription()
    {
        if (Status != SubscriptionStatus.Active) return;

        Status = SubscriptionStatus.Failed;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }
}