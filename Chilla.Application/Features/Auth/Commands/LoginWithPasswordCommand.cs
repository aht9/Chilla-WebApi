using Chilla.Application.Common.Interfaces;
using Chilla.Application.Features.Auth.DTOs;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using Chilla.Infrastructure.Authentication;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record LoginWithPasswordCommand(string Username, string Password, string IpAddress) : IRequest<AuthResult>;

public class LoginWithPasswordHandler : IRequestHandler<LoginWithPasswordCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher; // Injected

    public LoginWithPasswordHandler(
        IUserRepository userRepository, 
        IJwtTokenGenerator jwtGenerator, 
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResult> Handle(LoginWithPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        if (user == null || user.PasswordHash == null) 
            throw new Exception("نام کاربری یا کلمه عبور اشتباه است.");

        // Fixed: Use PasswordHasher service
        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        
        if (!isPasswordValid)
        {
            user.RecordLoginFailure();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new Exception("نام کاربری یا کلمه عبور اشتباه است.");
        }

        if (user.IsLockedOut) throw new Exception($"حساب کاربری تا {user.LockoutEnd} مسدود است.");
        if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

        user.ResetLoginStats();

        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");
        var refreshToken = _jwtGenerator.GenerateRefreshToken();

        user.AddRefreshToken(refreshToken, request.IpAddress);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, refreshToken, user.IsProfileCompleted(), "ورود موفقیت‌آمیز.");
    }
}