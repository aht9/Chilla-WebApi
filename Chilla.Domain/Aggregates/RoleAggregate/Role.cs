namespace Chilla.Domain.Aggregates.RoleAggregate;

public class Role : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    
    // نگهداری لیست دسترسی‌ها به صورت یک لیست از Enum
    private readonly List<RolePermission> _permissions = new();
    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private Role()
    {
        Id = Guid.NewGuid();
    }

    public Role(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
    }

    public void AssignPermission(Permission permission)
    {
        if (!_permissions.Any(p => p.Permission == permission))
        {
            _permissions.Add(new RolePermission(permission));
            UpdateAudit();
        }
    }

    public void RemovePermission(Permission permission)
    {
        var perm = _permissions.SingleOrDefault(p => p.Permission == permission);
        if (perm != null)
        {
            _permissions.Remove(perm);
            UpdateAudit();
        }
    }
}

public class RolePermission : BaseEntity
{
    public Permission Permission { get; private set; }

    private RolePermission() { }
    public RolePermission(Permission permission)
    {
        Permission = permission;
    }
}