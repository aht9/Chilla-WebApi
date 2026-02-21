namespace Chilla.Application.Features.Auth.DTOs;

public record ResetPasswordRequest(
    string PhoneNumber, 
    string Code, 
    string NewPassword, 
    string ConfirmNewPassword
);