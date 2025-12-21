namespace Chilla.Application.Features.Auth.DTOs;

public record ForgotPasswordRequest(string PhoneNumber);

public record ResetPasswordRequest(
    string PhoneNumber, 
    string Code, 
    string NewPassword, 
    string ConfirmNewPassword
);