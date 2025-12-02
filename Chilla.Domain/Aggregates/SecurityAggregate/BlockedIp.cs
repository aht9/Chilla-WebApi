namespace Chilla.Domain.Aggregates.SecurityAggregate;

public class BlockedIp : BaseEntity, IAggregateRoot
{
    public string IpAddress { get; private set; }
    public string Reason { get; private set; }
    public DateTime BlockedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; } // Null means permanent block
    public bool IsActive => (ExpiresAt == null || ExpiresAt > DateTime.UtcNow) && !IsDeleted;

    private BlockedIp() { }

    public BlockedIp(string ipAddress, string reason, double? durationInMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentNullException(nameof(ipAddress));
        
        IpAddress = ipAddress;
        Reason = reason;
        BlockedAt = DateTime.UtcNow;
        ExpiresAt = durationInMinutes.HasValue 
            ? DateTime.UtcNow.AddMinutes(durationInMinutes.Value) 
            : null;
    }

    public void LiftBlock()
    {
        ExpiresAt = DateTime.UtcNow; // Expire immediately
        UpdateAudit();
    }
}