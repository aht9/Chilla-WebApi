namespace Chilla.Domain.Aggregates.RoleAggregate;

// لیست تمام دسترسی‌های سامانه
public enum Permission
{
    // Users
    CanViewUsers = 1,
    CanBlockUsers = 2,
    CanEditUsers = 3,

    // Plans
    CanCreatePlans = 10,
    CanEditPlans = 11,
    CanDeletePlans = 12,

    // Security
    CanBlockIps = 20,
    CanViewSystemLogs = 21,
    
    // Admin
    SuperAdmin = 999
}