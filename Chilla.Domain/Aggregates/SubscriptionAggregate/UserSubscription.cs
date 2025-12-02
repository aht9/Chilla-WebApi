namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public enum SubscriptionStatus { Active, Completed, Failed, Canceled }

public class UserSubscription : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    
    // Tracks distinct daily progress
    private readonly List<DailyProgress> _progress = new();
    public IReadOnlyCollection<DailyProgress> Progress => _progress.AsReadOnly();

    private UserSubscription() { }

    public UserSubscription(Guid userId, Guid planId)
    {
        UserId = userId;
        PlanId = planId;
        StartDate = DateTime.UtcNow;
        Status = SubscriptionStatus.Active;
    }

    public void MarkTaskAsComplete(Guid planTemplateItemId, int valueEntered = 0)
    {
        if (Status != SubscriptionStatus.Active) throw new InvalidOperationException("Subscription is not active.");

        // Check if entry already exists for today
        var today = DateTime.UtcNow.Date;
        var existing = _progress.SingleOrDefault(p => p.PlanTemplateItemId == planTemplateItemId && p.Date.Date == today);

        if (existing != null)
        {
            existing.UpdateValue(valueEntered);
        }
        else
        {
            _progress.Add(new DailyProgress(planTemplateItemId, today, true, valueEntered));
        }
        UpdateAudit();
    }

    public void CompleteSubscription()
    {
        Status = SubscriptionStatus.Completed;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }
}