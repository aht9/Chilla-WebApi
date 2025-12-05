namespace Chilla.Application.Features.Auth.DTOs;

public record CompleteProfileRequest(
    Guid UserId, 
    string FirstName, 
    string LastName, 
    string Username, 
    string? Email, 
    string? Password
);