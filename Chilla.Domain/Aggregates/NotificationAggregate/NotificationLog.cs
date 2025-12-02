namespace Chilla.Domain.Aggregates.NotificationAggregate;

public class NotificationLog : BaseEntity
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Content { get; private set; }
    public string Target { get; private set; } // Phone number or Email
    public bool IsSent { get; private set; }
    public string? ErrorMessage { get; private set; }

    public NotificationLog(Guid userId, NotificationType type, string content, string target)
    {
        UserId = userId;
        Type = type;
        Content = content;
        Target = target;
    }

    public void MarkAsSent() => IsSent = true;
    public void MarkAsFailed(string error) => ErrorMessage = error;
}