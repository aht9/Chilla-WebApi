namespace Chilla.Domain.Aggregates.NotificationAggregate;

[Flags] 
public enum NotificationType
{
    None = 0,
    Site = 1,
    Sms = 2,
    Email = 4,
    Call = 8,
    Push = 16      // 10000 (برای موبایل)
}