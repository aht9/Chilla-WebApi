namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public enum SubscriptionStatus { Active, Completed, Failed, Canceled }

public class UserSubscription : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    
    // Concurrency check for progress updates is handled by BaseEntity.RowVersion
    private readonly List<DailyProgress> _progress = new();
    public IReadOnlyCollection<DailyProgress> Progress => _progress.AsReadOnly();

    private UserSubscription() { }

    public UserSubscription(Guid userId, Guid planId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");

        UserId = userId;
        PlanId = planId;
        StartDate = DateTime.UtcNow;
        Status = SubscriptionStatus.Active;
    }

    public void MarkTaskAsComplete(Guid planTemplateItemId, int valueEntered = 0)
    {
        if (Status != SubscriptionStatus.Active) 
            throw new InvalidOperationException("Cannot update progress on an inactive subscription.");

        // Validation: Ensure the task belongs to the plan (Ideally checked via service/query before calling, 
        // but here we ensure state consistency).
        
        var today = DateTime.UtcNow.Date;
        
        // Find existing progress for this task TODAY
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

    public void CancelSubscription()
    {
        if (Status == SubscriptionStatus.Completed) throw new InvalidOperationException("Cannot cancel a completed subscription.");
        
        Status = SubscriptionStatus.Canceled;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }

    public void CompleteSubscription()
    {
        if (Status != SubscriptionStatus.Active) return;

        Status = SubscriptionStatus.Completed;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }

    public void FailSubscription()
    {
         if (Status != SubscriptionStatus.Active) return;

        Status = SubscriptionStatus.Failed;
        EndDate = DateTime.UtcNow;
        UpdateAudit();
    }
}