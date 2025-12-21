using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chilla.Application.Features.Auth.Commands;

public record RequestOtpCommand(string PhoneNumber) : IRequest<bool>;

public class RequestOtpHandler : IRequestHandler<RequestOtpCommand, bool>
{
    private readonly IOtpService _otpService;
    private readonly ISmsSender _smsSender;
    
    private readonly ILogger<RequestOtpHandler> _logger;

    public RequestOtpHandler(IOtpService otpService, ISmsSender smsSender, ILogger<RequestOtpHandler> logger)
    {
        _otpService = otpService;
        _smsSender = smsSender;
        _logger = logger;
    }

    public async Task<bool> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        // تولید و کش کردن کد
        var code = await _otpService.GenerateAndCacheOtpAsync(request.PhoneNumber, "login", 2);
        // ارسال پیامک
        await _smsSender.SendAsync(request.PhoneNumber, $"کد ورود شما به چله: {code}");
        _logger.LogWarning("code :" + code);
        return true;
    }
}