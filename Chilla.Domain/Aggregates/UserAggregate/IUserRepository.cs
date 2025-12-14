namespace Chilla.Domain.Aggregates.UserAggregate;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    
    // متد برای بررسی وجود کاربر بدون واکشی کامل (Performance Optimization)
    Task<bool> ExistsByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    
    Task<bool> IsUsernameTakenAsync(string username, CancellationToken cancellationToken = default);
    
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    
    // در EF Core معمولاً Update نیازی به متد Async ندارد چون State در مموری ترک می‌شود
    void Update(User user);
    
    // متد Delete هم صرفاً State را تغییر می‌دهد
    void Delete(User user);
}