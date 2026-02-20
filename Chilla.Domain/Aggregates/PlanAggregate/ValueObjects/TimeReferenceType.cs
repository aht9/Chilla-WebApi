namespace Chilla.Domain.Aggregates.PlanAggregate.ValueObjects;

public enum TimeReferenceType
{
    FixedTime,      // ساعت مشخص (مثلا 14:00)
    RelativeToFajr, // نسبت به اذان صبح
    RelativeToZuhr, // نسبت به اذان ظهر
    RelativeToMaghrib, // نسبت به اذان مغرب
    Sunset,         // غروب
    Sunrise         // طلوع
}