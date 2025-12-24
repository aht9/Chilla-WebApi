using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Exceptions;

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
    
    /// <summary>
    /// متد اصلی برای افزودن آیتم‌ها که هوشمندانه از ورود داده‌های غیربهینه جلوگیری می‌کند.
    /// </summary>
    public void ReplaceTemplateItems(List<PlanTemplateItemInputModel> incomingItems)
    {
        // 1. پاکسازی لیست قبلی (اگر ویرایش کلی است) یا مدیریت افزودن
        _items.Clear(); 

        // 2. اعتبارسنجی هوشمند برای جلوگیری از افزونگی
        ValidateOptimization(incomingItems);

        // 3. افزودن آیتم‌ها
        foreach (var item in incomingItems)
        {
            // اعتبارسنجی بازه روزها نسبت به کل پلن
            if (item.EndDay > DurationInDays)
                throw new DomainException($"Task '{item.TaskName}' ends on day {item.EndDay} but plan duration is {DurationInDays}.");

            _items.Add(new PlanTemplateItem(
                item.StartDay, 
                item.EndDay, 
                item.TaskName, 
                item.Type, 
                item.ConfigJson, 
                item.IsMandatory, 
                item.NotificationType
            ));
        }
        
        UpdateAudit();
    }
    
    /// <summary>
    /// این متد بررسی می‌کند آیا مدیر به جای استفاده از بازه (Range)، 
    /// چندین رکورد پشت سر هم برای روزهای متوالی ثبت کرده است یا خیر.
    /// </summary>
    private static void ValidateOptimization(List<PlanTemplateItemInputModel> items)
    {
        // گروه بندی تسک‌هایی که دقیقاً ماهیت یکسان دارند (نام، کانفیگ، اجبار، نوتیفیکیشن)
        var similarTasksGroups = items
            .GroupBy(x => new { 
                x.TaskName, 
                x.Type, 
                x.ConfigJson, // مقایسه رشته جیسون (باید نرمالایز شده باشد)
                x.IsMandatory, 
                x.NotificationType 
            })
            .ToList();

        foreach (var group in similarTasksGroups)
        {
            // مرتب‌سازی بر اساس روز شروع
            var sortedRanges = group.OrderBy(x => x.StartDay).ToList();

            for (int i = 0; i < sortedRanges.Count - 1; i++)
            {
                var current = sortedRanges[i];
                var next = sortedRanges[i + 1];

                // همپوشانی (Overlap) که کلاً غلط است
                if (current.EndDay >= next.StartDay)
                {
                    throw new DomainException(
                        $"Optimization Error: Task '{current.TaskName}' has overlapping days ranges ({current.StartDay}-{current.EndDay}) and ({next.StartDay}-{next.EndDay}).");
                }

                // تشخیص عدم بهینگی: پایان این تسک دقیقاً متصل به شروع تسک بعدی است
                // مثال: تسک A (روز ۱ تا ۵) و تسک A (روز ۶ تا ۱۰)
                // این دو باید یکی شوند: تسک A (روز ۱ تا ۱۰)
                if (current.EndDay + 1 == next.StartDay)
                {
                    throw new DomainException(
                        $"Optimization Alert: You have defined '{current.TaskName}' specifically for days {current.StartDay}-{current.EndDay} and then again for {next.StartDay}-{next.EndDay}. " +
                        $"Since the settings are identical, please MERGE them into a single row: Day {current.StartDay} to {next.EndDay}. This keeps the plan optimized.");
                }
            }
        }
    }
    
    public static void ValidateForRedundancy(List<PlanTemplateItemInputModel> incomingItems)
    {
        // گروه بندی آیتم ها بر اساس شباهت کامل محتوا (به جز روز)
        var grouped = incomingItems
            .GroupBy(x => new { x.TaskName, x.Type, x.ConfigJson, x.IsMandatory, x.NotificationType })
            .ToList();

        foreach (var group in grouped)
        {
            // مرتب‌سازی روزها
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