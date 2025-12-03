using Chilla.Application.Common.Interfaces;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record CompleteProfileCommand(Guid UserId, string FirstName, string LastName, string Username, string? Email, string? Password) : IRequest<bool>;

public class CompleteProfileHandler : IRequestHandler<CompleteProfileCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher; // Injected

    public CompleteProfileHandler(
        IUserRepository userRepository, 
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> Handle(CompleteProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) throw new Exception("User not found.");

        if (user.Username != request.Username)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (existingUser != null) throw new Exception("این نام کاربری قبلاً گرفته شده است.");
        }

        user.CompleteProfile(request.FirstName, request.LastName, request.Username, request.Email);
        
        if (!string.IsNullOrEmpty(request.Password))
        {
            // Fixed: Use Hash Service
            var hashedPassword = _passwordHasher.HashPassword(request.Password);
            user.SetPassword(hashedPassword);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}