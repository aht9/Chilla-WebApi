namespace Chilla.Application.Services.Interface;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
}