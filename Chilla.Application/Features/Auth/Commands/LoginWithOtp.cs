using Chilla.Domain.Specifications.UserSpecs;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Chilla.Application.Features.Auth.Commands;

public record LoginWithOtpCommand(string PhoneNumber, string Code, string IpAddress) : IRequest<AuthResult>;

public record AuthResult(string AccessToken, string RefreshToken);

public class LoginWithOtpHandler : IRequestHandler<LoginWithOtpCommand, AuthResult>
{
    private readonly AppDbContext _context;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IDistributedCache _cache; // برای بررسی کد OTP

    public LoginWithOtpHandler(AppDbContext context, IJwtTokenGenerator jwtGenerator, IDistributedCache cache)
    {
        _context = context;
        _jwtGenerator = jwtGenerator;
        _cache = cache;
    }

    public async Task<AuthResult> Handle(LoginWithOtpCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate OTP from Redis
        var cachedOtp = await _cache.GetStringAsync($"otp:{request.PhoneNumber}");
        if (cachedOtp != request.Code)
        {
             throw new Exception("کد وارد شده نامعتبر یا منقضی شده است.");
             // اینجا باید AccessFailedCount کاربر را زیاد کنید (با فراخوانی متد دامین)
        }

        // 2. Find User via Specification
        var userSpec = new UserByPhoneSpec(request.PhoneNumber);
        var user = await SpecificationEvaluator.GetQuery(_context.Users, userSpec).SingleOrDefaultAsync(cancellationToken);

        if (user == null) throw new Exception("کاربری با این شماره یافت نشد.");
        if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

        // 3. Generate Tokens
        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");
        var refreshToken = _jwtGenerator.GenerateRefreshToken();

        // 4. Store Refresh Token in Aggregate (CRITICAL: Persistence)
        // لاجیک چرخش توکن: توکن‌های قدیمی را حذف و جدید را اضافه می‌کنیم
        user.AddRefreshToken(refreshToken, request.IpAddress, daysToExpire: 30);
        
        // پاک کردن OTP مصرف شده
        await _cache.RemoveAsync($"otp:{request.PhoneNumber}");

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, refreshToken);
    }
}