namespace Chilla.Application.Features.Auth.DTOs;

public record AuthResult(
    string AccessToken, 
    string RefreshToken, 
    bool IsProfileCompleted,
    string? Message = null
);