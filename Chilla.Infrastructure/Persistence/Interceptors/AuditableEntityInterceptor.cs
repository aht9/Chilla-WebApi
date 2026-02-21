using Chilla.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Chilla.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            // ۱. اگر رکورد در حال ویرایش است، فقط تاریخ آپدیت را ثبت کن
            if (entry.State == EntityState.Modified)
            {
                // فرض بر این است که UpdateAudit در BaseEntity تاریخ UpdatedAt را تنظیم می‌کند
                entry.Entity.UpdateAudit(); 
            }
            // ۲. اگر دستور حذف (Remove) صادر شده است، جلوی آن را بگیر
            else if (entry.State == EntityState.Deleted)
            {
                // وضعیت را از حذف به ویرایش تغییر بده (تا در دیتابیس بماند)
                entry.State = EntityState.Modified;
                
                // متد Delete که در BaseEntity نوشتید را صدا بزنید تا IsDeleted=true شود
                entry.Entity.Delete(); 
            }
        }
    }
}