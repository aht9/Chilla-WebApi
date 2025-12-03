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

    public LoginWithPasswordHandler(IUserRepository userRepository, IJwtTokenGenerator jwtGenerator, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _jwtGenerator = jwtGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResult> Handle(LoginWithPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);

        // در پروداکشن نباید بگوییم یوزر پیدا نشد یا پسورد غلط است، پیام باید کلی باشد
        if (user == null || user.PasswordHash == null) 
            throw new Exception("نام کاربری یا کلمه عبور اشتباه است.");

        // TODO: Password Hashing Verify Logic (Here assuming simple string compare for brevity, use BCrypt/Argon2 in prod)
        bool isPasswordValid = ("Hashed_" + request.Password) == user.PasswordHash; 
        
        if (!isPasswordValid)
        {
            user.RecordLoginFailure();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new Exception("نام کاربری یا کلمه عبور اشتباه است.");
        }

        if (user.IsLockedOut) throw new Exception($"حساب کاربری تا {user.LockoutEnd} مسدود است.");
        if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

        // Reset Login Failures on success
        user.ResetLoginStats();

        // Generate Tokens
        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");
        var refreshToken = _jwtGenerator.GenerateRefreshToken();

        user.AddRefreshToken(refreshToken, request.IpAddress);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(accessToken, refreshToken, user.IsProfileCompleted(), "ورود موفقیت‌آمیز.");
    }
}