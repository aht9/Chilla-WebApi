namespace Chilla.Domain.Aggregates.PlanAggregate;

public class Plan : BaseEntity, IAggregateRoot
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; } // 0 = Free
    public int DurationInDays { get; private set; }
    public bool IsActive { get; private set; }

    // List of tasks/actions per day template
    private readonly List<PlanTemplateItem> _items = new();
    public IReadOnlyCollection<PlanTemplateItem> Items => _items.AsReadOnly();

    private Plan() { }

    public Plan(string title, string description, decimal price, int durationInDays)
    {
        Title = title;
        Description = description;
        Price = price;
        DurationInDays = durationInDays;
        IsActive = true;
    }

    public void AddTaskTemplate(int dayNumber, string taskName, TaskType type, string configJson, bool isMandatory)
    {
        if (dayNumber > DurationInDays) throw new ArgumentException("Day number exceeds plan duration");
        
        _items.Add(new PlanTemplateItem(dayNumber, taskName, type, configJson, isMandatory));
        UpdateAudit();
    }
    
}