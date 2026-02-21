using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Services;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record ForgotPasswordCommand(string PhoneNumber) : IRequest<ResultSampleResetPasswordCode>;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ResultSampleResetPasswordCode>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;

    public ForgotPasswordHandler(IUserRepository userRepository, IOtpService otpService, ISmsSender smsSender)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _smsSender = smsSender;
    }

    public async Task<ResultSampleResetPasswordCode> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. بررسی وجود کاربر
        // به دلایل امنیتی، حتی اگر کاربر وجود نداشت، نباید خطا برگردانیم (User Enumeration)
        // اما فعلاً برای سادگی چک می‌کنیم.
        var userExists = await _userRepository.ExistsByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        
        if (!userExists)
        {
            // شبیه‌سازی تاخیر برای جلوگیری از تایمینگ اتک
            await Task.Delay(500, cancellationToken);
            return new ResultSampleResetPasswordCode
            {
                Result = true
            }; // به کاربر می‌گوییم ارسال شد (حتی اگر نشد)
        }

        // 2. تولید کد با هدف "reset-password"
        var code = await _otpService.GenerateAndCacheOtpAsync(request.PhoneNumber, "reset-password", 2);

        // 3. ارسال پیامک
        await _smsSender.SendAsync(request.PhoneNumber, $"کد تغییر رمز عبور: {code}");

        return new ResultSampleResetPasswordCode
        {
            Result = true,
            Code = code
        };
    }
}


public class ResultSampleResetPasswordCode
{
    public bool Result { get; set; }
    public string Code { get; set; }
}