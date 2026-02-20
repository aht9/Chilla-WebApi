using Chilla.Domain.Aggregates.NotificationAggregate;

namespace Chilla.Domain.Aggregates.PlanAggregate;


public enum TaskType
{
    Boolean,    // انجام شد/نشد (مثل نماز شب)
    Counter,    // ذکر شمار (نیاز به TargetCount دارد)
    TimeBound,  // زمانی (مثل بیداری قبل از ۵ صبح)
    Reading     // خواندن متن (نمایش متن)
}

public class PlanTemplateItem : BaseEntity
{
    public int StartDay { get; private set; } 
    public int EndDay { get; private set; }
    public string TaskName { get; private set; }
    public TaskType Type { get; private set; }
    
    // تنظیمات نوتیفیکیشن اختصاصی برای این تسک
    public NotificationType RequiredNotifications { get; private set; }

    // JSON برای تنظیمات مذهبی (مثل: ۱ ساعت قبل از اذان)
    public string ConfigJson { get; private set; } 
    public bool IsMandatory { get; private set; }

    private PlanTemplateItem() { }

    public PlanTemplateItem(int startDay, int endDay, string taskName, TaskType type, string configJson, bool isMandatory, NotificationType requiredNotifications)
    {
        if (startDay > endDay) throw new ArgumentException("Start day cannot be after end day.");
        if (startDay < 1) throw new ArgumentException("Start day must be at least 1.");

        StartDay = startDay;
        EndDay = endDay;
        TaskName = taskName;
        Type = type;
        ConfigJson = configJson;
        IsMandatory = isMandatory;
        RequiredNotifications = requiredNotifications;
    }

    // متد کمکی برای بررسی اینکه آیا این آیتم شامل روز خاصی می‌شود یا خیر
    public bool CoversDay(int day)
    {
        return day >= StartDay && day <= EndDay;
    }
}