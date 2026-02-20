using System.Text.Json;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Infrastructure.Common;
using Chilla.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Chilla.Application.Features.Users.Commands;

public record RegisterUserCommand(string FirstName, string LastName, string Username, string Phone, string Password, string? Email) : IRequest<Guid>;

public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, Guid>
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher; 

    public RegisterUserHandler(AppDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken);
        
        if (exists) throw new Exception("Username already exists.");

        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var user = new User(request.FirstName, request.LastName, request.Username, request.Phone, request.Email);
        user.SetPassword(hashedPassword);
        _context.Users.Add(user);
        
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);
        if (defaultRole != null)
        {
            user.AssignRole(defaultRole.Id);
        }

        var evt = new { UserId = user.Id, Email = user.Email, Phone = user.PhoneNumber };
        _context.OutboxMessages.Add(new OutboxMessage 
        { 
            Id = Guid.NewGuid(),
            Type = "UserRegisteredEvent", 
            Content = JsonSerializer.Serialize(evt),
            OccurredOn = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}