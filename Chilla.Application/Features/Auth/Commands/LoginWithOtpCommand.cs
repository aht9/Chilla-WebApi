using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Chilla.Application.Features.Auth.Commands;

public record LoginWithOtpCommand(string PhoneNumber, string Code, string IpAddress) : IRequest<AuthResult>;

public class LoginWithOtpHandler : IRequestHandler<LoginWithOtpCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<LoginWithOtpHandler> _logger;

    public LoginWithOtpHandler(
        IUserRepository userRepository,
        IOtpService otpService,
        IJwtTokenGenerator jwtGenerator,
        IUnitOfWork unitOfWork,
        AppDbContext dbContext,
        ILogger<LoginWithOtpHandler> logger)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(LoginWithOtpCommand request, CancellationToken cancellationToken)
    {
        var isValid = await _otpService.ValidateOtpAsync(request.PhoneNumber, request.Code);
        //بررسی سناریو بلاک
        if (!isValid)
        {
            var failCount = await _otpService.IncrementOtpFailureCountAsync(request.PhoneNumber);
            var userToCheck = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);

            if (userToCheck != null)
            {
                // سناریوی ۱: کاربر قبلاً لاک بوده و حالا "فرصت آخر" (OTP) را هم خراب کرده
                if (userToCheck.IsLockedOut)
                {
                    // جریمه سنگین: بلاک ۲۴ ساعته
                    userToCheck.LockoutUntil(DateTimeOffset.UtcNow.AddHours(24));
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogWarning("User {Phone} double-blocked for 24h due to failed OTP while locked.", request.PhoneNumber);
                    throw new Exception("حساب کاربری شما به دلیل تلاش‌های ناموفق مکرر (رمز عبور و پیامک) به مدت ۲۴ ساعت مسدود شد.");
                }

                // سناریوی ۲: ۳ بار اشتباه در وارد کردن OTP
                if (failCount >= 3)
                {
                    userToCheck.LockoutUntil(DateTimeOffset.UtcNow.AddMinutes(20)); // بلاک ۲۰ دقیقه‌ای
                    await _otpService.ResetOtpFailureCountAsync(request.PhoneNumber); // ریست شمارنده برای دور بعد
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    _logger.LogWarning("User {Phone} blocked for 20m due to 3 failed OTP attempts.", request.PhoneNumber);
                    throw new Exception("حساب شما به دلیل ۳ بار ورود اشتباه کد پیامک، به مدت ۲۰ دقیقه مسدود شد.");
                }
            }

            throw new OtpValidationException($"کد نامعتبر است. تعداد تلاش‌های ناموفق: {failCount}");
        }
        
        
        const int MaxRetries = 3;
        int attempt = 0;

        while (true)
        {
            try
            {
                attempt++;
                return await ProcessLoginAsync(request, cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (attempt >= MaxRetries)
                {
                    _logger.LogError(ex, "Concurrency error persisting login for {Phone} after {Retries} attempts.",
                        request.PhoneNumber, MaxRetries);
                    throw; 
                }

                _logger.LogWarning("Concurrency conflict for {Phone}. Retrying attempt {Attempt}...",
                    request.PhoneNumber, attempt);
            
                _dbContext.ChangeTracker.Clear();
                await Task.Delay(Random.Shared.Next(50, 150), cancellationToken);
            }
        }
    }

    private async Task<AuthResult> ProcessLoginAsync(LoginWithOtpCommand request, CancellationToken cancellationToken)
    {

        var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
        bool isNewUser = false;

        if (user == null)
        {
            user = new User(request.PhoneNumber);
            var defaultRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
            if (defaultRole != null)
            {
                user.AssignRole(defaultRole.Id);
            }
            await _userRepository.AddAsync(user, cancellationToken);
            isNewUser = true;
        }
        else
        {
            if (user.IsLockedOut)
            {
                user.ResetLoginStats();
                _logger.LogInformation("User {Phone} unlocked via successful OTP.", request.PhoneNumber);
            }

            // ریست کردن شمارنده خطاهای OTP در Redis (چون لاگین موفق بود)
            await _otpService.ResetOtpFailureCountAsync(request.PhoneNumber);
        }

        if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

        var userRoleName = user.Roles.Any() ? "User" : "User"; 
        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, userRoleName);
        var refreshToken = _jwtGenerator.GenerateRefreshToken();
        
        user.AddRefreshToken(refreshToken, request.IpAddress);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            IsProfileCompleted: user.IsProfileCompleted(),
            Message: isNewUser ? "ثبت نام اولیه انجام شد." : "ورود با موفقیت انجام شد."
        );
    }
}