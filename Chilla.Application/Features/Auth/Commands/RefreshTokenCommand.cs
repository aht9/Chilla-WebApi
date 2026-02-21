using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Authentication;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken, string IpAddress) : IRequest<AuthResult>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public RefreshTokenHandler(IUserRepository userRepository, IJwtTokenGenerator jwtGenerator, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. جستجوی کاربر به همراه توکن‌ها و نقش‌ها
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user == null)
            throw new UnauthorizedAccessException("توکن نامعتبر است.");

        var existingToken = user.RefreshTokens.Single(t => t.Token == request.RefreshToken);

        // 2. تشخیص استفاده مجدد (Reuse Detection) - امنیت سطح بالا
        if (existingToken.IsRevoked)
        {
            user.RevokeAllRefreshTokens(request.IpAddress, "Security Alert: Reused Revoked Token");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            throw new UnauthorizedAccessException("هشدار امنیتی: تلاش برای استفاده از توکن نامعتبر.");
        }

        if (existingToken.IsExpired)
            throw new UnauthorizedAccessException("توکن منقضی شده است. لطفاً مجدداً وارد شوید.");

        // استخراج نقش‌های کاربر
        var userRoleNames = user.Roles.Select(ur => ur.Role.Name).ToList();
        if (!userRoleNames.Any()) userRoleNames.Add("User");

        // 3. چرخش توکن (Rotation)
        var newRefreshToken = _jwtGenerator.GenerateRefreshToken();
        // ارسال لیست نقش‌ها به جای یک رشته ثابت
        var newAccessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, userRoleNames);

        user.RotateRefreshToken(existingToken, newRefreshToken, request.IpAddress);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(newAccessToken, newRefreshToken, user.IsProfileCompleted(), "تمدید موفقیت‌آمیز.");
    }
}