using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Authentication;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record LoginWithOtpCommand(string PhoneNumber, string Code, string IpAddress) : IRequest<AuthResult>;

public class LoginWithOtpHandler : IRequestHandler<LoginWithOtpCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public LoginWithOtpHandler(
        IUserRepository userRepository, 
        IOtpService otpService, 
        IJwtTokenGenerator jwtGenerator, 
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> Handle(LoginWithOtpCommand request, CancellationToken cancellationToken)
    {
        // 1. اعتبارسنجی کد OTP
        var isValid = await _otpService.ValidateOtpAsync(request.PhoneNumber, request.Code);
        if (!isValid)
        {
            throw new Exception("کد وارد شده نامعتبر یا منقضی شده است.");
        }

        // 2. بررسی وجود کاربر
        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        bool isNewUser = false;

        if (user == null)
        {
            // سناریوی ۲: ثبت‌نام نکرده است -> ایجاد کاربر جدید
            user = new User(request.PhoneNumber);
            await _userRepository.AddAsync(user, cancellationToken);
            isNewUser = true;
        }

        if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

        // 3. تولید توکن‌ها
        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User"); // Default role
        var refreshToken = _jwtGenerator.GenerateRefreshToken();

        // 4. ذخیره رفرش توکن
        user.AddRefreshToken(refreshToken, request.IpAddress);

        // 5. ذخیره تغییرات (چه ثبت‌نام جدید، چه لاگین کاربر قدیمی)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. بازگشت نتیجه
        return new AuthResult(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            IsProfileCompleted: user.IsProfileCompleted(),
            Message: isNewUser ? "ثبت نام اولیه انجام شد. لطفاً پروفایل را تکمیل کنید." : "ورود با موفقیت انجام شد."
        );
    }
}