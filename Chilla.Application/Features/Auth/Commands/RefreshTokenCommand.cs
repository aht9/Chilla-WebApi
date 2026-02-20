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
    private readonly AppDbContext _context;
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
        // 1. جستجوی کاربر بر اساس توکن (بهینه شده)
        // ما نیاز داریم توکن‌ها را هم لود کنیم تا بتوانیم چک کنیم
        var user = await _context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == request.RefreshToken), cancellationToken);

        if (user == null)
            throw new UnauthorizedAccessException("توکن نامعتبر است.");

        var existingToken = user.RefreshTokens.Single(t => t.Token == request.RefreshToken);

        // 2. اعتبارسنجی توکن
        if (!existingToken.IsActive)
        {
            // امنیتی: اگر کسی سعی کرد از توکن سوخته استفاده کند، شاید دزدی توکن رخ داده!
            // در سناریوهای خیلی حساس، همه توکن‌های کاربر را باطل می‌کنند.
            throw new UnauthorizedAccessException("توکن منقضی یا باطل شده است.");
        }

        // 3. چرخش توکن (Token Rotation) - امنیت بالا
        // توکن قبلی را باطل می‌کنیم
        user.RevokeRefreshToken(request.RefreshToken, request.IpAddress, "Replaced by new token");

        // توکن جدید صادر می‌کنیم
        var newAccessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");
        var newRefreshToken = _jwtGenerator.GenerateRefreshToken();

        user.AddRefreshToken(newRefreshToken, request.IpAddress);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(newAccessToken, newRefreshToken, user.IsProfileCompleted(), "توکن تمدید شد.");
    }
}