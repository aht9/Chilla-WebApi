namespace Chilla.Application.Features.Users.Dtos;

public record UserProfileDto(
    Guid Id, 
    string FirstName, 
    string LastName, 
    string Username, 
    string PhoneNumber
);