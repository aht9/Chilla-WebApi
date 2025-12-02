namespace Chilla.Domain.Aggregates.UserAggregate;

public class UserRefreshToken : BaseEntity
{
    public string Token { get; private set; }
    public DateTime Expires { get; private set; }
    public DateTime Created { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTime? Revoked { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? ReasonRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => Revoked != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private UserRefreshToken() { }

    public UserRefreshToken(string token, DateTime expires, string createdByIp)
    {
        Token = token;
        Expires = expires;
        CreatedByIp = createdByIp;
        Created = DateTime.UtcNow;
    }

    public void Revoke(string ipAddress, string reason)
    {
        Revoked = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReasonRevoked = reason;
    }
}