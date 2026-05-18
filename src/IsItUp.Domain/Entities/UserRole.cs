using System.Security;

namespace IsItUp.Domain.Entities;

public class UserRole
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }           // "Admin", "Member", "Viewer"
    public IReadOnlyCollection<Permission> Permissions { get; }
}
