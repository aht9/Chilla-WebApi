using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record CompleteProfileCommand(Guid UserId, string FirstName, string LastName, string Username, string? Email, string? Password) : IRequest<bool>;

public class CompleteProfileHandler : IRequestHandler<CompleteProfileCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteProfileHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CompleteProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null) throw new Exception("User not found.");

        // چک کردن یکتایی Username جدید اگر تغییر کرده باشد
        if (user.Username != request.Username)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
            if (existingUser != null) throw new Exception("این نام کاربری قبلاً گرفته شده است.");
        }

        user.CompleteProfile(request.FirstName, request.LastName, request.Username, request.Email);
        
        if (!string.IsNullOrEmpty(request.Password))
        {
            // TODO: Use Hash Service
            user.SetPassword("Hashed_" + request.Password);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}