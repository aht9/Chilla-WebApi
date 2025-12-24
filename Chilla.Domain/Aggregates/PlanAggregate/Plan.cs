using Chilla.Domain.Aggregates.NotificationAggregate;

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

    public void AddTaskTemplate(int startDay, int endDay, string taskName, TaskType type, string configJson, bool isMandatory, NotificationType notifications)
    {
        if (endDay > DurationInDays) 
            throw new ArgumentOutOfRangeException(nameof(endDay), $"End day cannot exceed plan duration ({DurationInDays}).");
        
        // 1. بررسی همپوشانی منطقی یا تکراری بودن (به صورت ساده)
        // این بخش چک می‌کند که دقیقاً همین تسک قبلاً برای این بازه اد نشده باشد
        var isDuplicate = _items.Any(i => 
            i.TaskName == taskName && 
            i.Type == type &&
            i.StartDay == startDay && 
            i.EndDay == endDay);

        if (isDuplicate)
            throw new InvalidOperationException($"Duplicate task '{taskName}' specifically for days {startDay}-{endDay} already exists.");

        _items.Add(new PlanTemplateItem(startDay, endDay, taskName, type, configJson, isMandatory, notifications));
        UpdateAudit();
    }
    
    public static void ValidateForRedundancy(List<PlanTemplateItemInputModel> incomingItems)
    {
        // گروه بندی آیتم ها بر اساس شباهت کامل محتوا (به جز روز)
        var grouped = incomingItems
            .GroupBy(x => new { x.TaskName, x.Type, x.ConfigJson, x.IsMandatory, x.NotificationType })
            .ToList();

        foreach (var group in grouped)
        {
            // مرتب سازی روزها
            var sortedDays = group.OrderBy(x => x.StartDay).ToList();
            
            for (int i = 0; i < sortedDays.Count - 1; i++)
            {
                var current = sortedDays[i];
                var next = sortedDays[i + 1];

                // اگر تسک فعلی تمام شد و بلافاصله تسک بعدی (که دقیقا مشابه است) شروع شد
                // یعنی مدیر به جای Range، دو تا ردیف جدا زده است.
                // مثال: آیتم ۱ (روز ۱ تا ۱) - آیتم ۲ (روز ۲ تا ۲) -> باید ادغام شوند
                if (current.EndDay + 1 == next.StartDay)
                {
                    throw new InvalidOperationException(
                        $"Optimization Warning: The task '{current.TaskName}' is repeated on consecutive days ({current.EndDay} and {next.StartDay}). " +
                        $"Please use a single task with a Day Range (StartDay to EndDay) instead of multiple rows to save data.");
                }
            }
        }
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

public record PlanTemplateItemInputModel(int StartDay, int EndDay, string TaskName, TaskType Type, string ConfigJson, bool IsMandatory, NotificationType NotificationType);