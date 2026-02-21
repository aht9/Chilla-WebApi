using Chilla.Domain.Aggregates.RoleAggregate;

namespace Chilla.Domain.Aggregates.UserAggregate;


public class UserRole : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public Role Role { get; private set; } = null!;

    private UserRole() { } 

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public UserRole(Guid roleId)
    {
        RoleId = roleId;
    }
}