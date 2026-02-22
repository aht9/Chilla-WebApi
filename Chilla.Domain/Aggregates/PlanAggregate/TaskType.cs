namespace Chilla.Domain.Aggregates.PlanAggregate;

public enum TaskType
{
    Boolean,        // انجام شد/نشد ساده
    Counter,        // ذکر شمار (نیاز به TargetCount دارد)
    Reading,        // خواندنی (متن دعا، سوره و...)
    PhysicalAction, // اقدام فیزیکی/مناسک (غسل کردن، نوشتن نامه، پاشیدن آب)
    Consumable,     // خوردنی/آشامیدنی (آب متبرک، سیب و...)
    Donation,       // مالی و انفاق (صدقه، رد مظالم)
    MultiStep       // چند مرحله‌ای (ترکیبی از موارد بالا)
}