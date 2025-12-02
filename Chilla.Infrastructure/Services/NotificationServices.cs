using Microsoft.Extensions.Logging;

namespace Chilla.Infrastructure.Services;

// اینترفیس‌ها معمولاً در لایه Application هستند، اینجا فرض می‌کنیم وجود دارند یا همینجا تعریف می‌کنیم
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message);
}

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string body);
}

public class SmsSender : ISmsSender
{
    private readonly ILogger<SmsSender> _logger;

    public SmsSender(ILogger<SmsSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string phoneNumber, string message)
    {
        // TODO: Integrate with KavehNegar, Twilio, or Magfa here
        _logger.LogInformation("SMS Sent to {Phone}: {Message}", phoneNumber, message);
        await Task.CompletedTask;
    }
}

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ILogger<EmailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(string email, string subject, string body)
    {
        // TODO: Integrate with SMTP or SendGrid
        _logger.LogInformation("Email Sent to {Email}: {Subject}", email, subject);
        await Task.CompletedTask;
    }
}