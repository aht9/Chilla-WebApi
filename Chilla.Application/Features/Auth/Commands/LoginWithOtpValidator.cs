using FluentValidation;

namespace Chilla.Application.Features.Auth.Commands;

public class LoginWithOtpValidator : AbstractValidator<LoginWithOtpCommand>
{
    public LoginWithOtpValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Matches(@"^09\d{9}$").WithMessage("فرمت شماره موبایل صحیح نیست (مثال: 0912...)");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("کد تایید الزامی است.")
            .Length(5).WithMessage("کد تایید باید ۵ رقم باشد.")
            .Matches(@"^\d+$").WithMessage("کد تایید فقط شامل اعداد است.");
    }
}