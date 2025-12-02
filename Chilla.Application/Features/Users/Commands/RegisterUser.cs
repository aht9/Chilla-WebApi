using System.Text.Json;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Infrastructure.Persistence;
using MediatR;

namespace Chilla.Application.Features.Users.Commands;

public record RegisterUserCommand(string FirstName, string LastName, string Username, string Phone, string Password, string? Email) : IRequest<Guid>;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly AppDbContext _context;
    // No Repo injected, using DbContext directly with Logic

    public RegisterUserHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check Uniqueness (Using Spec)
        var existingUserSpec = new UserByUsernameSpec(request.Username);
        var exists = await SpecificationEvaluator.GetQuery(_context.Users, existingUserSpec).AnyAsync(cancellationToken);
        
        if (exists) throw new Exception("Username already exists.");

        // 2. Create Aggregate
        // Note: Password hashing should happen via a service, omitted for brevity
        var user = new User(request.FirstName, request.LastName, request.Username, request.Phone, "Hashed_" + request.Password, request.Email);

        // 3. Add to Context
        _context.Users.Add(user);

        // 4. Add Outbox Message (Domain Event: UserRegistered)
        var evt = new { UserId = user.Id, Email = user.Email, Phone = user.Phone };
        _context.OutboxMessages.Add(new OutboxMessage 
        { 
            Id = Guid.NewGuid(),
            Type = "UserRegisteredEvent",
            Content = JsonSerializer.Serialize(evt),
            OccurredOn = DateTime.UtcNow
        });

        // 5. Save Changes (Atomic Transaction)
        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}