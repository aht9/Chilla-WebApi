using System.Text.Json;
using Chilla.Application.Common.Interfaces;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Users.Commands;

public record RegisterUserCommand(string FirstName, string LastName, string Username, string Phone, string Password, string? Email) : IRequest<Guid>;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher; // Injected

    public RegisterUserHandler(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Check Uniqueness (Manual check via EF or Spec)
        // Using EF direct check for brevity here as per previous code style
        var exists = await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken);
        
        if (exists) throw new Exception("Username already exists.");

        // 2. Hash Password
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        // 3. Create Aggregate
        var user = new User(request.FirstName, request.LastName, request.Username, request.Phone, request.Email);
        user.SetPassword(hashedPassword); // Set password separately

        // 4. Add to Context
        _context.Users.Add(user);

        // 5. Add Outbox Message
        var evt = new { UserId = user.Id, Email = user.Email, Phone = user.PhoneNumber };
        _context.OutboxMessages.Add(new OutboxMessage 
        { 
            Id = Guid.NewGuid(),
            Type = "UserRegisteredEvent", // Should match assembly qualified name in real implementation
            Content = JsonSerializer.Serialize(evt),
            OccurredOn = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}