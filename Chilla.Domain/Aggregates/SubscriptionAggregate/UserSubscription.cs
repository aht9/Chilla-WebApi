namespace Chilla.Domain.Aggregates.SubscriptionAggregate;

public enum SubscriptionStatus { Active, Completed, Failed, Canceled, PendingPayment }

public class UserSubscription : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public Guid PlanId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    
    private readonly List<DailyProgress> _progress = new();
    public IReadOnlyCollection<DailyProgress> Progress => _progress.AsReadOnly();

    private UserSubscription()
    {
        Id = Guid.NewGuid();
    }

    public UserSubscription(Guid userId, Guid planId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = planId;
        StartDate = DateTime.UtcNow;
        Status = SubscriptionStatus.Active;
    }

    public UserSubscription(Guid userId, Guid planId, DateTime startDate, DateTime endDate, bool requiresPayment)
    {
        if (userId == Guid.Empty) throw new ArgumentException("UserId required");
        if (planId == Guid.Empty) throw new ArgumentException("PlanId required");
        if (endDate < startDate) throw new ArgumentException("EndDate cannot be before StartDate");

        Id = Guid.NewGuid();
        UserId = userId;
        PlanId = planId;
        StartDate = startDate;
        EndDate = endDate;
        
        Status = requiresPayment ? SubscriptionStatus.PendingPayment : SubscriptionStatus.Active;
    }

    public void Activate()
    {
        if (Status == SubscriptionStatus.PendingPayment)
        {
            Status = SubscriptionStatus.Active;
            UpdateAudit();
        }
    }

    public void MarkTaskAsComplete(Guid planTemplateItemId, int valueEntered = 0)
    {
        if (Status != SubscriptionStatus.Active) 
            throw new InvalidOperationException("Cannot update progress on an inactive subscription.");

        var today = DateTime.UtcNow.Date;
        
        var existing = _progress.SingleOrDefault(p => p.PlanTemplateItemId == planTemplateItemId && p.ScheduledDate.Date == today);

        if (existing != null)
        {
            existing.UpdateValue(valueEntered, false, null);
        }
        else
        {
            _progress.Add(new DailyProgress(planTemplateItemId, today, valueEntered));
        }
        
        UpdateAudit();
    }
    
    public void MarkTaskAsCompleteWithCommitment(Guid planTemplateItemId, int valueEntered, string commitmentReason)
    {
        if (Status != SubscriptionStatus.Active) 
            throw new InvalidOperationException("Cannot update progress on an inactive subscription.");

        var today = DateTime.UtcNow.Date;
        
        var existing = _progress.SingleOrDefault(p => p.PlanTemplateItemId == planTemplateItemId && p.ScheduledDate.Date == today);

        if (existing != null)
        {
            existing.UpdateValue(valueEntered, true, commitmentReason);
        }
        else
        {
            _progress.Add(new DailyProgress(planTemplateItemId, today, valueEntered, true, commitmentReason));
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