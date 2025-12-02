namespace Chilla.Domain.Aggregates.SecurityAggregate;

public class RequestLog : BaseEntity
{
    public string IpAddress { get; private set; }
    public string Endpoint { get; private set; }
    public DateTime OccurredOn { get; private set; }

    public RequestLog(string ipAddress, string endpoint)
    {
        IpAddress = ipAddress;
        Endpoint = endpoint;
        OccurredOn = DateTime.UtcNow;
    }
}