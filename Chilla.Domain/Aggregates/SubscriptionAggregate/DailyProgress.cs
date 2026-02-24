namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public class DailyProgress : BaseEntity
{
    public Guid SubscriptionId { get; private set; } // کلید خارجی به اشتراک کاربر
    public Guid TaskId { get; private set; }         // همان PlanTemplateItemId
    public int DayNumber { get; private set; }       // روز چندم چله است؟ (مثلا 1 تا 40)
    
    public bool IsDone { get; private set; }         // برای تسک‌های تیک‌زدنی (مثل خواندن سوره)
    public int CountCompleted { get; private set; }  // برای تسک‌های شمارشی (مثل 33 بار ذکر)
    
    public DateTime? CompletedAt { get; private set; } // زمان دقیق ثبت در سیستم
    
    // --- فیلدهای سناریوی تعهدنامه ---
    public bool IsLateEntry { get; private set; }    // آیا این رکورد مربوط به روزهای گذشته بوده که با تعهدنامه باز شده است؟

    private DailyProgress() { }

    public DailyProgress(Guid subscriptionId, Guid taskId, int dayNumber, bool isDone, int countCompleted, bool isLateEntry)
    {
        SubscriptionId = subscriptionId;
        TaskId = taskId;
        DayNumber = dayNumber;
        IsDone = isDone;
        CountCompleted = countCompleted;
        IsLateEntry = isLateEntry;
        
        // اگر تسک انجام شده تلقی شود، زمان ثبت را ذخیره می‌کنیم
        CompletedAt = isDone || countCompleted > 0 ? DateTime.UtcNow : null;
    }

    public void UpdateProgress(bool isDone, int countCompleted, bool isLateEntry)
    {
        IsDone = isDone;
        CountCompleted = countCompleted;
        
        // بروزرسانی زمان انجام
        CompletedAt = isDone || countCompleted > 0 ? DateTime.UtcNow : null;
        
        // نکته منطقی: اگر این تسک قبلاً به عنوان "تاخیری" (با تعهدنامه) ثبت شده باشد،
        // حتی اگر امروز ویرایش شود، همچنان مهر "تاخیری" روی آن باقی می‌ماند.
        if (isLateEntry)
        {
            IsLateEntry = true;
        }
    }
}