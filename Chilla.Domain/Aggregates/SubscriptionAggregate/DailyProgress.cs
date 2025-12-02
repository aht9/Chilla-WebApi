namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public class DailyProgress : BaseEntity
{
    public Guid PlanTemplateItemId { get; private set; } // Reference to what task was done
    public DateTime Date { get; private set; }
    public bool IsCompleted { get; private set; }
    
    // Value stores the "result".
    // If Counter: store the count (e.g., 100).
    // If Boolean: store 1 or 0.
    public int Value { get; private set; }

    private DailyProgress() { }

    public DailyProgress(Guid planTemplateItemId, DateTime date, bool isCompleted, int value)
    {
        PlanTemplateItemId = planTemplateItemId;
        Date = date;
        IsCompleted = isCompleted;
        Value = value;
    }

    public void UpdateValue(int newValue)
    {
        Value = newValue;
        // Logic can be added here to un-complete if value < target
    }
}