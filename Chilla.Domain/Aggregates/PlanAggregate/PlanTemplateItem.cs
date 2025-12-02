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
    public int DayNumber { get; private set; } // 1 to 40
    public string TaskName { get; private set; }
    public TaskType Type { get; private set; }
    
    // JSON Configuration for flexibility.
    // Example for Counter: { "target": 100, "step_text": "Ya Allah" }
    // Example for TimeBound: { "before_time": "05:30" }
    public string ConfigJson { get; private set; } 
    public bool IsMandatory { get; private set; }

    private PlanTemplateItem() { }

    public PlanTemplateItem(int dayNumber, string taskName, TaskType type, string configJson, bool isMandatory)
    {
        DayNumber = dayNumber;
        TaskName = taskName;
        Type = type;
        ConfigJson = configJson;
        IsMandatory = isMandatory;
    }
}