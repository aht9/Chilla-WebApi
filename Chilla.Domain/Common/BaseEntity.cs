using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Chilla.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; set; } // Concurrency Token

    public void UpdateAudit() => UpdatedAt = DateTime.UtcNow;
    public void Delete() { IsDeleted = true; UpdateAudit(); }

    // --- Domain Events Support ---
    private readonly List<IDomainEvent> _domainEvents = new();

    [NotMapped] // EF Core shouldn't map this to DB column
    [JsonIgnore]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

public interface IAggregateRoot { }