namespace IsItUp.Domain.Entities;

public class Permission
{
    public string Resource { get; private set; }       // "monitors", "alerts", "team"
    public string Action { get; private set; }         // "read", "write", "delete", "admin"
}