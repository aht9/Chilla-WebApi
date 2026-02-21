using Chilla.Infrastructure.Authentication;
using Chilla.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chilla.Application.Features.Auth.Commands;

public record RequestOtpCommand(string PhoneNumber) : IRequest<ResponseOtpSample>;

public class RequestOtpHandler : IRequestHandler<RequestOtpCommand, ResponseOtpSample>
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

    public async Task<ResponseOtpSample> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        // تولید و کش کردن کد
        var code = await _otpService.GenerateAndCacheOtpAsync(request.PhoneNumber, "login", 2);
        // ارسال پیامک
        await _smsSender.SendAsync(request.PhoneNumber, $"کد ورود شما به چله: {code}");
        _logger.LogWarning("code :" + code);
        return new ResponseOtpSample
        {
            Result =  true,
            Code = code,
        };
    }
    
}


public class ResponseOtpSample
{
    public bool Result { get; set; }
    public string Code { get; set; }
}