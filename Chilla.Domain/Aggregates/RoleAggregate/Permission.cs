namespace Chilla.Domain.Aggregates.RoleAggregate;

// لیست تمام دسترسی‌های سامانه
public enum Permission
{
    // --- Users (مدیریت کاربران) ---
    CanViewUsers = 1,
    CanBlockUsers = 2,
    CanEditUsers = 3,

    // --- Plans & Subscription (پلن‌ها و اشتراک) ---
    CanViewPlans = 10,          // عمومی: مشاهده لیست پلن‌ها
    CanPurchasePlan = 11,       // عمومی: امکان خرید
    CanViewMySubscription = 12, // کاربر دارای اشتراک: مشاهده کارت خود
    CanManageMySubscription = 13, // کاربر دارای اشتراک: ثبت تیک روزانه/تعهد
    
    // --- Admin Plans (مدیریت پلن‌ها توسط ادمین) ---
    CanCreatePlans = 15,
    CanEditPlans = 16,
    CanDeletePlans = 17,

    // --- Tickets (پشتیبانی) ---
    CanViewMyTickets = 30,      // کاربر: دیدن تیکت‌های خود
    CanCreateTicket = 31,       // کاربر: ایجاد تیکت جدید
    CanReplyTicket = 32,        // کاربر: پاسخ به تیکت
    CanManageAllTickets = 33,   // ادمین: دیدن و پاسخ به همه تیکت‌ها

    // --- Security (امنیت) ---
    CanBlockIps = 50,
    CanViewSystemLogs = 51,
    
    // --- Admin Root ---
    SuperAdmin = 999
}