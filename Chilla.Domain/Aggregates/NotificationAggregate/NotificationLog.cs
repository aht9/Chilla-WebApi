namespace Chilla.Domain.Aggregates.NotificationAggregate;

public class NotificationLog : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Content { get; private set; }
    public string Target { get; private set; } // Phone number, Email, or DeviceToken
    public bool IsSent { get; private set; }
    public DateTime? SentAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    private NotificationLog() { }

    public NotificationLog(Guid userId, NotificationType type, string content, string target)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("Content cannot be empty.", nameof(content));
        if (string.IsNullOrWhiteSpace(target)) throw new ArgumentException("Target cannot be empty.", nameof(target));

        UserId = userId;
        Type = type;
        Content = content;
        Target = target;
        IsSent = false;
        RetryCount = 0;
    }

    public void MarkAsSent()
    {
        IsSent = true;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
        UpdateAudit();
    }

    public void MarkAsFailed(string error)
    {
        IsSent = false;
        ErrorMessage = error;
        RetryCount++;
        UpdateAudit();
    }
}