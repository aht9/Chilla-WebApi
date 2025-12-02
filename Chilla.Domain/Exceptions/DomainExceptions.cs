namespace Chilla.Domain.Exceptions;

public class SubscriptionDomainException : Exception
{
    public SubscriptionDomainException(string message) : base(message) { }
}

public class TaskTimeWindowMissedException : SubscriptionDomainException
{
    public DateTime Deadline { get; }
    public TaskTimeWindowMissedException(DateTime deadline) 
        : base($"زمان انجام این فعالیت به پایان رسیده است (مهلت: {deadline:HH:mm}). لطفاً از گزینه ثبت با تعهد استفاده کنید.")
    {
        Deadline = deadline;
    }
}

public class DayLockedException : SubscriptionDomainException
{
    public DateTime TargetDate { get; }
    public DayLockedException(DateTime targetDate) 
        : base($"امکان ویرایش فعالیت‌های تاریخ {targetDate:yyyy/MM/dd} وجود ندارد زیرا روز به پایان رسیده است.")
    {
        TargetDate = targetDate;
    }
}