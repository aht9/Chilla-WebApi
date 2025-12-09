using Chilla.Domain.Aggregates.NotificationAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Domain.Aggregates.UserAggregate.Events;
using Chilla.Infrastructure.Persistence;
using Chilla.Infrastructure.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Chilla.Application.Features.Users.Events;

// این کلاس در لایه Application قرار می‌گیرد و توسط MediatR وقتی OutboxProcessor پیام را Publish کرد صدا زده می‌شود
public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly AppDbContext _dbContext;
    private readonly ISmsSender _smsSender;
    private readonly IUserRepository _userRepository;
    // private readonly IEmailSender _emailSender; 

    public UserRegisteredEventHandler(AppDbContext dbContext, ISmsSender smsSender, IUserRepository userRepository)
    {
        _dbContext = dbContext;
        _smsSender = smsSender;
        _userRepository = userRepository;
    }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user == null)
        {
            return;
        }

        // 1. ارسال پیامک خوش‌آمدگویی
        string message = $"سلام {user.FirstName} عزیز، به چله خوش آمدید!";
        bool sent = false;
        string? error = null;

        try
        {
            // اگر سرویس‌دهنده پیامک خارجی است، حتما در try-catch باشد تا کل پروسه فیل نشود
            // یا از Polly برای Retry استفاده شود
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                await _smsSender.SendAsync(user.PhoneNumber, message);
                sent = true;
            }
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        // 2. ثبت لاگ نوتیفیکیشن در دیتابیس (برای تاریخچه و دیباگ)
        var log = new NotificationLog(
            userId: user.Id,
            type: NotificationType.Sms,
            content: message,
            target: user.PhoneNumber
        );

        if (sent) log.MarkAsSent();
        else log.MarkAsFailed(error ?? "Unknown Error");

        _dbContext.NotificationLogs.Add(log);

        // 3. ذخیره تغییرات
        // نکته: این SaveChanges مستقل از تراکنش اصلی ثبت کاربر است و در Background Job انجام می‌شود.
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}