using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Common;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record ResetPasswordCommand(string PhoneNumber, string Code, string NewPassword, string ConfirmNewPassword) : IRequest<bool>;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordHandler(
        IUserRepository userRepository, 
        IOtpService otpService, 
        IUnitOfWork unitOfWork, 
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. اعتبارسنجی ساده پسوردها
        if (request.NewPassword != request.ConfirmNewPassword)
            throw new Exception("رمز عبور و تکرار آن مطابقت ندارند.");

        // 2. اعتبارسنجی کد با هدف "reset-password"
        var isValid = await _otpService.ValidateOtpAsync(request.PhoneNumber, request.Code, "reset-password");
        if (!isValid)
        {
            // افزایش خطا و ... (مشابه لاگین)
            await _otpService.IncrementOtpFailureCountAsync(request.PhoneNumber);
            throw new OtpValidationException("کد نامعتبر یا منقضی شده است.");
        }

        // 3. دریافت کاربر
        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        if (user == null) throw new Exception("کاربر یافت نشد.");

        // 4. تغییر رمز
        var hashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPassword(hashedPassword);

        // 5. امنیتی: ابطال تمام توکن‌های فعال (کاربر باید دوباره لاگین کند)
        // (اختیاری ولی توصیه می‌شود)
        // user.RevokeAllRefreshTokens("Password Changed");

        // 6. ذخیره
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // پاک کردن خطاهای OTP
        await _otpService.ResetOtpFailureCountAsync(request.PhoneNumber);

        return true;
    }
}