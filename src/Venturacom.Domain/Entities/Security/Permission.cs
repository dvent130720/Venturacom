using Venturacom.Domain.Enums;

namespace Venturacom.Domain.Entities.Security;

public sealed class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public ModuleType Module { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
