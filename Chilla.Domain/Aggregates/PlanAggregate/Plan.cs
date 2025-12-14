namespace Chilla.Domain.Aggregates.PlanAggregate;

public class Plan : BaseEntity, IAggregateRoot
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; } 
    public int DurationInDays { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<PlanTemplateItem> _items = new();
    public IReadOnlyCollection<PlanTemplateItem> Items => _items.AsReadOnly();

    private Plan()
    {
        Id = Guid.NewGuid();
    }

    public Plan(string title, string description, decimal price, int durationInDays)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required");
        if (durationInDays <= 0) throw new ArgumentException("Duration must be greater than 0");
        if (price < 0) throw new ArgumentException("Price cannot be negative");

        Id = Guid.NewGuid();
        Title = title;
        Description = description;
        Price = price;
        DurationInDays = durationInDays;
        IsActive = true;
    }

    public void AddTaskTemplate(int dayNumber, string taskName, TaskType type, string configJson, bool isMandatory)
    {
        if (dayNumber < 1 || dayNumber > DurationInDays) 
            throw new ArgumentOutOfRangeException(nameof(dayNumber), $"Day number must be between 1 and {DurationInDays}");
        
        if (string.IsNullOrWhiteSpace(taskName))
            throw new ArgumentNullException(nameof(taskName));

        // Note: In a real scenario, we might want to validate 'configJson' schema here based on 'type'
        
        _items.Add(new PlanTemplateItem(dayNumber, taskName, type, configJson, isMandatory));
        UpdateAudit();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateAudit();
    }
    
    public void Activate()
    {
        IsActive = true;
        UpdateAudit();
    }
}