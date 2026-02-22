namespace Chilla.Domain.Aggregates.PlanAggregate;

public enum FrequencyType
{
    Daily,          // هر روز در بازه مشخص شده
    Weekly,         // X روز در هفته
    Interval,       // هر X روز یکبار (مثلا هر 3 روز)
    Once            // فقط یکبار در کل بازه (مثل روز اول یا روز آخر)
}