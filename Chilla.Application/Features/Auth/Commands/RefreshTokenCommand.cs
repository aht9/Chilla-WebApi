using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken, string IpAddress) : IRequest<AuthResult>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly AppDbContext _context; // Direct access specifically for complex Auth query
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenHandler(AppDbContext context, IJwtTokenGenerator jwtGenerator, IUnitOfWork unitOfWork)
    {
        _context = context;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. جستجوی کاربر به همراه توکن‌ها
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.RefreshToken), cancellationToken);

        if (user == null)
            throw new UnauthorizedAccessException("توکن نامعتبر است.");

        var existingToken = user.RefreshTokens.Single(t => t.Token == request.RefreshToken);

        // 2. تشخیص استفاده مجدد (Reuse Detection) - امنیت سطح بالا
        if (existingToken.IsRevoked)
        {
            // کسی دارد از توکن سوخته استفاده می‌کند! احتمالاً توکن دزدیده شده است.
            // اقدام امنیتی: همه توکن‌های این کاربر را باطل کن تا مجبور شود دوباره لاگین کند.
            user.RevokeAllRefreshTokens(request.IpAddress, "Security Alert: Reused Revoked Token");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            throw new UnauthorizedAccessException("هشدار امنیتی: تلاش برای استفاده از توکن نامعتبر.");
        }

        if (existingToken.IsExpired)
            throw new UnauthorizedAccessException("توکن منقضی شده است. لطفاً مجدداً وارد شوید.");

        // 3. چرخش توکن (Rotation)
        // توکن فعلی را باطل می‌کنیم و لینک می‌دهیم به توکن جدید
        var newRefreshToken = _jwtGenerator.GenerateRefreshToken();
        var newAccessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");

        // متد RevokeRefreshToken باید در Domain آپدیت شود تا ReplacedByToken را پشتیبانی کند
        user.RotateRefreshToken(existingToken, newRefreshToken, request.IpAddress);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(newAccessToken, newRefreshToken, user.IsProfileCompleted(), "تمدید موفقیت‌آمیز.");
    }
}