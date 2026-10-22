using Knect.Domain.Common;
using Knect.Domain.Enums;

namespace Knect.Domain.Entities;

public class Role : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public UserRoleType RoleType { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
