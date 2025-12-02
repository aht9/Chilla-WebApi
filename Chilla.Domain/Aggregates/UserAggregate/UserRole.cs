namespace Chilla.Domain.Aggregates.UserAggregate;

public class UserRole : BaseEntity
{
    public Guid RoleId { get; private set; }

    private UserRole() { }
    public UserRole(Guid roleId)
    {
        RoleId = roleId;
    }
}