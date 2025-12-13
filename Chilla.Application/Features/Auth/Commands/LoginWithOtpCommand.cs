using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Domain.Exceptions;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Auth.Commands;

public record LoginWithOtpCommand(string PhoneNumber, string Code, string IpAddress) : IRequest<AuthResult>;

public class LoginWithOtpHandler : IRequestHandler<LoginWithOtpCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _dbContext;

    public LoginWithOtpHandler(
        IUserRepository userRepository,
        IOtpService otpService,
        IJwtTokenGenerator jwtGenerator,
        IUnitOfWork unitOfWork, AppDbContext dbContext)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
    }

   public async Task<AuthResult> Handle(LoginWithOtpCommand request, CancellationToken cancellationToken)
{
    // اعتبارسنجی اولیه (نیازی به تکرار در حلقه ندارد)
    var isValid = await _otpService.ValidateOtpAsync(request.PhoneNumber, request.Code);
    if (!isValid) throw new OtpValidationException("کد نامعتبر است.");

    int maxRetries = 3;
    int currentRetry = 0;

    while (true)
    {
        try
        {
            // 2. بررسی وجود کاربر
            var user = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber, cancellationToken);
            bool isNewUser = false;

            if (user == null)
            {
                user = new User(request.PhoneNumber);
                await _userRepository.AddAsync(user, cancellationToken);
                isNewUser = true;
            }

            if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

            // 3. تولید توکن‌ها
            var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");
            var refreshToken = _jwtGenerator.GenerateRefreshToken();

            // 4. افزودن رفرش توکن
            user.AddRefreshToken(refreshToken, request.IpAddress);

            // 5. ذخیره تغییرات
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 6. موفقیت
            return new AuthResult(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                IsProfileCompleted: user.IsProfileCompleted(),
                Message: isNewUser
                    ? "ثبت نام اولیه انجام شد."
                    : "ورود با موفقیت انجام شد."
            );
        }
        catch (DbUpdateConcurrencyException)
        {
            currentRetry++;
            if (currentRetry >= maxRetries) throw;

            _dbContext.ChangeTracker.Clear();

            await Task.Delay(50, cancellationToken);
        }
    }
}
}