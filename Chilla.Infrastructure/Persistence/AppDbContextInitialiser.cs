using Chilla.Domain.Aggregates.RoleAggregate;
using Chilla.Domain.Aggregates.UserAggregate;
using Chilla.Infrastructure.Common;
using Microsoft.Extensions.Logging;

namespace Chilla.Infrastructure.Persistence;

public class AppDbContextInitialiser
{
    private readonly ILogger<AppDbContextInitialiser> _logger;
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AppDbContextInitialiser(
        ILogger<AppDbContextInitialiser> logger,
        AppDbContext context,
        IPasswordHasher passwordHasher)
    {
        _logger = logger;
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task InitialiseAsync()
    {
        try
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.MigrateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // 1. ایجاد نقش‌ها و دسترسی‌ها
        // ---------------------------------------------------

        // A. نقش SuperAdmin
        var adminRole = await _context.Roles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == "SuperAdmin");

        if (adminRole == null)
        {
            adminRole = new Role("SuperAdmin", "دسترسی کامل به تمام امکانات سیستم");
            _context.Roles.Add(adminRole);
        }

        // اختصاص تمام دسترسی‌های موجود به SuperAdmin
        foreach (var permission in Enum.GetValues<Permission>())
        {
            adminRole.AssignPermission(permission);
        }

        // B. نقش User (کاربر عادی)
        var userRole = await _context.Roles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == "User");

        if (userRole == null)
        {
            userRole = new Role("User", "کاربر عادی ثبت‌نام شده");
            _context.Roles.Add(userRole);
        }

        // اختصاص دسترسی‌های محدود به کاربر عادی طبق سناریو
        // (مشاهده پلن‌ها، خرید پلن، دسترسی به تیکت‌ها)
        var userPermissions = new[]
        {
            Permission.CanViewPlans,
            Permission.CanPurchasePlan,
            Permission.CanViewMyTickets,
            Permission.CanCreateTicket,
            Permission.CanReplyTicket,
            Permission.CanViewMySubscription, // کاربر می‌تواند اشتراک خود را ببیند (اگر داشته باشد)
            Permission.CanManageMySubscription
        };

        foreach (var perm in userPermissions)
        {
            userRole.AssignPermission(perm);
        }

        // 2. ایجاد کاربر SuperAdmin پیش‌فرض (اگر وجود نداشت)
        // ---------------------------------------------------
        var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            // پسورد: Admin123!
            var passwordHash = _passwordHasher.HashPassword("Admin123!");

            adminUser = new User("مدیر", "سیستم", "admin", "09120000000", "admin@chilla.ir");
            adminUser.SetPassword(passwordHash);
            adminUser.AssignRole(adminRole.Id); // دادن نقش ادمین به این کاربر

            _context.Users.Add(adminUser);
        }

        await _context.SaveChangesAsync();
    }
}