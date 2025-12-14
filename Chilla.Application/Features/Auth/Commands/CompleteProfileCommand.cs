using Chilla.Application.Services.Interface;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Common;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record CompleteProfileCommand(
    string FirstName,
    string LastName,
    string Username,
    string? Email,
    string? Password
) : IRequest<bool>;

public class CompleteProfileHandler : IRequestHandler<CompleteProfileCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;

    public CompleteProfileHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(CompleteProfileCommand request, CancellationToken cancellationToken)
    {
        // 1. دریافت ID کاربر از توکن امن (نه از ورودی کاربر)
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            throw new UnauthorizedAccessException("کاربر شناسایی نشد.");
        }

        // 2. واکشی کاربر واقعی
        var user = await _userRepository.GetByIdAsync(currentUserId.Value, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("کاربر یافت نشد.");
        }

        // 3. اعتبارسنجی نام کاربری (اگر تغییر کرده باشد)
        // اگر نام کاربری عوض نشده باشد، نیازی به چک تکراری بودن نیست
        var newUsername = request.Username.Trim();
        if (!string.Equals(user.Username, newUsername, StringComparison.OrdinalIgnoreCase))
        {
            var isTaken = await _userRepository.IsUsernameTakenAsync(newUsername, cancellationToken);
            if (isTaken) 
            {
                throw new Exception("این نام کاربری قبلاً توسط شخص دیگری گرفته شده است.");
            }
        }

        // 4. اعمال تغییرات روی Aggregate
        user.CompleteProfile(request.FirstName, request.LastName, request.Username, request.Email);

        // 5. تنظیم پسورد (در صورت ارسال)
        if (!string.IsNullOrEmpty(request.Password))
        {
            var hashedPassword = _passwordHasher.HashPassword(request.Password);
            user.SetPassword(hashedPassword);
        }

        // 6. ذخیره نهایی
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}