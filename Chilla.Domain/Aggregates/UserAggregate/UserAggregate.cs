namespace Chilla.Domain.Aggregates.UserAggregate;

public class User : BaseEntity, IAggregateRoot
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Username { get; private set; }
    public string PhoneNumber { get; private set; } // Main Identity for OTP
    public string? Email { get; private set; }      // Nullable
    public string? PasswordHash { get; private set; } // Nullable if OTP only
    public bool IsActive { get; private set; } = true;

    // Security
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }

    // Authentication Tokens
    private readonly List<UserRefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<UserRefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { } // EF Core

    public User(string firstName, string lastName, string username, string phoneNumber, string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        Username = username;
        PhoneNumber = phoneNumber;
        Email = email;
    }

    // --- Profile Actions ---
    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdateAudit();
    }

    public void UpdateProfile(string firstName, string lastName, string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        UpdateAudit();
    }

    // --- Security Actions ---
    public void RecordLoginFailure()
    {
        AccessFailedCount++;
        if (AccessFailedCount >= 3)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(20);
        }
        UpdateAudit();
    }

    public void ResetLoginStats()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        UpdateAudit();
    }

    // --- Token Management ---
    public void AddRefreshToken(string token, string remoteIp, double daysToExpire = 7)
    {
        // Remove old tokens to keep table clean (Optional logic)
        _refreshTokens.RemoveAll(t => !t.IsActive && t.Created.AddDays(daysToExpire + 2) < DateTime.UtcNow);
        
        _refreshTokens.Add(new UserRefreshToken(token, DateTime.UtcNow.AddDays(daysToExpire), remoteIp));
    }

    public bool RevokeRefreshToken(string token, string ipAddress, string reason = null)
    {
        var existingToken = _refreshTokens.SingleOrDefault(t => t.Token == token);
        if (existingToken != null && existingToken.IsActive)
        {
            existingToken.Revoke(ipAddress, reason);
            return true;
        }
        return false;
    }
}