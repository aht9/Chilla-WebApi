namespace Chilla.Application.Features.Auth.DTOs;

public record LoginResponseDto(
    bool IsProfileCompleted,
    string Message
);