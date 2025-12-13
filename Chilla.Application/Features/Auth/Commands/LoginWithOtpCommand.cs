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
        if (!isValid) throw new OtpValidationException("کد نامعتبر است.");

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
            await _userRepository.AddAsync(user, cancellationToken);
            isNewUser = true;
        }

        if (!user.IsActive) throw new Exception("حساب کاربری غیرفعال است.");

        var accessToken = _jwtGenerator.GenerateAccessToken(user.Id, user.Username, "User");
        var refreshToken = _jwtGenerator.GenerateRefreshToken();


        user.AddRefreshToken(refreshToken, request.IpAddress);

        var newTokenEntity = user.RefreshTokens.FirstOrDefault(t => t.Token == refreshToken);
        if (newTokenEntity != null)
        {
            _dbContext.Entry(newTokenEntity).State = EntityState.Added;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResult(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            IsProfileCompleted: user.IsProfileCompleted(),
            Message: isNewUser ? "ثبت نام اولیه انجام شد." : "ورود با موفقیت انجام شد."
        );
    }
}