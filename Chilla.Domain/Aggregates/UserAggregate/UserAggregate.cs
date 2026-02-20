using Chilla.Domain.Aggregates.UserAggregate.Events;

namespace Chilla.Domain.Aggregates.UserAggregate;

public class User : BaseEntity, IAggregateRoot
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Username { get; private set; }
    public string PhoneNumber { get; private set; }
    public string? Email { get; private set; }
    public string? PasswordHash { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Security & Lockout
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }

    // Notification Settings (Simplified as boolean flags for now, could be ValueObject)
    public bool IsSmsNotificationEnabled { get; private set; } = true;
    public bool IsEmailNotificationEnabled { get; private set; } = true;

    private readonly List<UserRefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<UserRefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    // افزودن کالکشن نقش‌ها
    private readonly List<UserRole> _roles = new();
    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    // سازنده برای ثبت‌نام سریع با شماره موبایل
    public User(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentNullException(nameof(phoneNumber));
        PhoneNumber = phoneNumber;
        Username = phoneNumber; // موقتاً نام کاربری همان شماره تلفن است
        IsActive = true;
        
        AddDomainEvent(new UserRegisteredEvent(this));
    }

    public User(string firstName, string lastName, string username, string phoneNumber, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentNullException(nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentNullException(nameof(lastName));
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentNullException(nameof(username));
        if (string.IsNullOrWhiteSpace(phoneNumber)) throw new ArgumentNullException(nameof(phoneNumber));

        FirstName = firstName;
        LastName = lastName;
        Username = username;
        PhoneNumber = phoneNumber;
        Email = email;
        IsActive = true;
        AddDomainEvent(new UserRegisteredEvent(this));
    }

    // --- Profile Management ---
    public bool IsProfileCompleted()
    {
        return !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);
    }
    
    public void CompleteProfile(string firstName, string lastName, string username, string? email)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("نام الزامی است");
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("نام خانوادگی الزامی است");
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("نام کاربری الزامی است");

        FirstName = firstName;
        LastName = lastName;
        Username = username;
        Email = email;
        
        UpdateAudit();
        AddDomainEvent(new UserProfileUpdatedEvent(this.Id));
    }
    
    
    public void UpdateProfile(string firstName, string lastName, string? email)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email; // Nullable allowed
        UpdateAudit();
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdateAudit();
    }

    public void ToggleActivity(bool isActive)
    {
        IsActive = isActive;
        UpdateAudit();
    }

    public void UpdateNotificationSettings(bool smsEnabled, bool emailEnabled)
    {
        IsSmsNotificationEnabled = smsEnabled;
        IsEmailNotificationEnabled = emailEnabled;
        UpdateAudit();
    }

    // --- Security Logic ---
    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    public void RecordLoginFailure()
    {
        AccessFailedCount++;
        if (AccessFailedCount >= 3)
        {
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(20); // Block duration
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
    public void AddRefreshToken(string token, string remoteIp, double daysToExpire = 30)
    {
        // Clean up old/invalid tokens to prevent table bloat
        var invalidTokens = _refreshTokens
            .Where(t => !t.IsActive && t.Created.AddDays(daysToExpire + 2) < DateTime.UtcNow).ToList();
        foreach (var t in invalidTokens) _refreshTokens.Remove(t);

        _refreshTokens.Add(new UserRefreshToken(token, DateTime.UtcNow.AddDays(daysToExpire), remoteIp));
    }

    public bool ValidatePassword(string passwordHashToCompare)
    {
        // در واقعیت باید هش پسورد ورودی را چک کنیم، اینجا فرض مقایسه هش شده است
        return PasswordHash == passwordHashToCompare; 
    }
    
    
    public bool RevokeRefreshToken(string token, string ipAddress, string reason)
    {
        var existingToken = _refreshTokens.SingleOrDefault(t => t.Token == token);
        if (existingToken != null && existingToken.IsActive)
        {
            existingToken.Revoke(ipAddress, reason);
            return true;
        }

        return false;
    }

    // متد برای افزودن نقش به کاربر
    public void AssignRole(Guid roleId)
    {
        if (!_roles.Any(r => r.RoleId == roleId))
        {
            _roles.Add(new UserRole(roleId));
            UpdateAudit();
        }
    }

    // متد برای حذف نقش از کاربر
    public void RemoveRole(Guid roleId)
    {
        var role = _roles.SingleOrDefault(r => r.RoleId == roleId);
        if (role != null)
        {
            _roles.Remove(role);
            UpdateAudit();
        }
    }
    
    // متد لاگین موفق (به‌روزرسانی آمار)
    public void RecordLoginSuccess(string ipAddress, string refreshToken, double refreshTokenExpiryDays = 30)
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
        AddRefreshToken(refreshToken, ipAddress, refreshTokenExpiryDays);
        // اینجا ایونت لاگین هم می‌توان منتشر کرد
        UpdateAudit();
    }
}