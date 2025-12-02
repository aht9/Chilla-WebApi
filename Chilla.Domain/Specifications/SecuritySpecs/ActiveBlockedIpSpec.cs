using Chilla.Domain.Aggregates.SecurityAggregate;

namespace Chilla.Domain.Specifications.SecuritySpecs;

public class ActiveBlockedIpSpec : BaseSpecification<BlockedIp>
{
    public ActiveBlockedIpSpec(string ipAddress) 
        : base(b => b.IpAddress == ipAddress && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow) && !b.IsDeleted)
    {
    }
}