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

    // --- فیلد جدید: وضعیت امضای تعهدنامه برای روزهای گذشته ---
    public bool HasSignedCovenant { get; private set; }

    private readonly List<DailyProgress> _dailyProgresses = new();
    public IReadOnlyCollection<DailyProgress> DailyProgresses => _dailyProgresses.AsReadOnly();

    private UserSubscription()
    {
        Id = Guid.NewGuid();
    }

    public UserSubscription(Guid userId, Guid planId, Guid? invoiceId, bool requiresPayment)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = planId;
        InvoiceId = invoiceId;
        StartDate = DateTime.UtcNow;
        HasSignedCovenant = false; // پیش‌فرض تعهدنامه‌ای امضا نشده است

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
        HasSignedCovenant = false;

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

    /// <summary>
    /// ثبت یا بروزرسانی پیشرفت یک تسک با پشتیبانی از زنجیره پیوسته و تاخیر (تعهدنامه)
    /// </summary>
    public void RecordTaskProgress(Guid taskId, int dayNumber, bool isDone, int countCompleted, bool isLateEntry, bool requiresUnbrokenChain)
    {
        // ۱. بررسی وضعیت کلی چله
        if (Status != SubscriptionStatus.Active)
            throw new DomainException("نمی‌توانید پیشرفت را در یک چله غیرفعال یا پایان‌یافته ثبت کنید.");

        // ۲. منطق بررسی زنجیره پیوسته (Unbroken Chain)
        // اگر تسک نیاز به پیوستگی دارد و روز اول نیستیم، باید چک کنیم روز قبل انجام شده باشد
        if (requiresUnbrokenChain && dayNumber > 1)
        {
            int previousDay = dayNumber - 1;
            
            var previousDayProgress = _dailyProgresses.FirstOrDefault(p => 
                p.TaskId == taskId && p.DayNumber == previousDay);

            // اگر روز قبل هیچ رکوردی ندارد، یا رکوردی دارد ولی کامل نشده است
            if (previousDayProgress == null || (!previousDayProgress.IsDone && previousDayProgress.CountCompleted == 0))
            {
                // زنجیره شکسته شده است! چله باطل می‌شود.
                FailSubscription();
                throw new DomainException(
                    $"زنجیره این تسک شکسته شد! شما تسک مربوط به روز {previousDay} را انجام نداده‌اید. " +
                    "طبق قوانین این چله، در صورت قطع شدن زنجیره، چله باطل شده و باید از ابتدا شروع کنید.");
            }
        }

        // ۳. پیدا کردن رکورد فعلی یا ایجاد رکورد جدید
        var existingProgress = _dailyProgresses.FirstOrDefault(p => 
            p.TaskId == taskId && p.DayNumber == dayNumber);

        if (existingProgress != null)
        {
            // بروزرسانی رکوردی که از قبل برای این روز وجود داشته
            existingProgress.UpdateProgress(isDone, countCompleted, isLateEntry);
        }
        else
        {
            // ثبت پیشرفت جدید
            _dailyProgresses.Add(new DailyProgress(Id, taskId, dayNumber, isDone, countCompleted, isLateEntry));
        }

        UpdateAudit();
    }

    /// <summary>
    /// امضای تعهدنامه توسط کاربر (برای باز کردن قفل ویرایش روزهای گذشته)
    /// </summary>
    public void SignRetroactiveCovenant()
    {
        if (Status != SubscriptionStatus.Active)
            throw new DomainException("تعهدنامه فقط برای چله‌های فعال قابل امضا است.");

        if (HasSignedCovenant)
            throw new DomainException("شما قبلاً تعهدنامه این چله را امضا کرده‌اید.");

        HasSignedCovenant = true;
        UpdateAudit();
    }

    // ==========================================
    // متدهای مدیریت وضعیت چله (Status Management)
    // ==========================================

    public void CancelSubscription()
    {
        if (Status == SubscriptionStatus.Completed || Status == SubscriptionStatus.Failed)
            throw new DomainException("این چله قبلاً پایان یافته است و قابل لغو نیست.");

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