using Chilla.Domain.Aggregates.UserAggregate;

namespace Chilla.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Roles) // Eager load roles if needed logic depends on it
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        // اینجا شاید نیاز باشد RefreshTokens را هم لود کنیم اگر برای لاگین استفاده می‌شود
        return await _context.Users
            .Include(u => u.RefreshTokens) 
            .SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Delete(User user)
    {
        // در صورت Soft Delete، متد Delete در خود Aggregate صدا زده شده و IsDeleted ترو شده است.
        // اینجا فقط آپدیت می‌کنیم یا اگر Hard Delete بخواهیم Remove می‌کنیم.
        // با توجه به BaseEntity و Soft Delete:
        _context.Users.Update(user); 
    }
}