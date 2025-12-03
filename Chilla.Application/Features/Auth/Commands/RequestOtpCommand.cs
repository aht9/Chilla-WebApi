using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Services;
using MediatR;

namespace Chilla.Application.Features.Auth.Commands;

public record RequestOtpCommand(string PhoneNumber) : IRequest<bool>;

public class RequestOtpHandler : IRequestHandler<RequestOtpCommand, bool>
{
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;

    public RequestOtpHandler(IOtpService otpService, ISmsSender smsSender)
    {
        _otpService = otpService;
        _smsSender = smsSender;
    }

    public async Task<bool> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        // تولید و کش کردن کد
        var code = await _otpService.GenerateAndCacheOtpAsync(request.PhoneNumber);

        // ارسال پیامک
        await _smsSender.SendAsync(request.PhoneNumber, $"کد ورود شما به چله: {code}");

        return true;
    }
}