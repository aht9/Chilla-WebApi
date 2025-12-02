namespace Chilla.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; set; } // Concurrency

    public void UpdateAudit() => UpdatedAt = DateTime.UtcNow;
    public void Delete() { IsDeleted = true; UpdateAudit(); }
}

public interface IAggregateRoot { }