namespace Chilla.Application.Features.Auth.DTOs;

public record LoginOtpRequest(string PhoneNumber, string Code);