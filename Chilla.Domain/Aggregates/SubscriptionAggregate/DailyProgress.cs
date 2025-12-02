namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public class DailyProgress : BaseEntity
{
    public Guid PlanTemplateItemId { get; private set; }
    public DateTime ScheduledDate { get; private set; } // تاریخی که باید انجام میشد
    public DateTime? CompletedAt { get; private set; }  // لحظه واقعی انجام
    public bool IsCompleted { get; private set; }
    public int Value { get; private set; }
    
    // --- فیلدهای جدید برای سناریوی تعهد ---
    public bool IsLateEntry { get; private set; } // آیا با تأخیر/تعهد ثبت شده؟
    public string? LateReason { get; private set; } // متن تعهد یا دلیل

    private DailyProgress() { }

    public DailyProgress(Guid planTemplateItemId, DateTime scheduledDate, int value, bool isLateEntry = false, string? lateReason = null)
    {
        PlanTemplateItemId = planTemplateItemId;
        ScheduledDate = scheduledDate.Date; // فقط تاریخ مهم است
        CompletedAt = DateTime.UtcNow;
        IsCompleted = true; // فعلاً فرض بر تکمیل است مگر منطق شمارنده متفاوت باشد
        Value = value;
        IsLateEntry = isLateEntry;
        LateReason = lateReason;
    }

    public void UpdateValue(int newValue, bool isLateEntry, string? lateReason)
    {
        Value = newValue;
        CompletedAt = DateTime.UtcNow;
        
        // اگر قبلاً Late بوده، وضعیتش حفظ می‌شود مگر اینکه آپدیت جدید هم Late باشد
        if (isLateEntry)
        {
            IsLateEntry = true;
            LateReason = lateReason;
        }
    }
}