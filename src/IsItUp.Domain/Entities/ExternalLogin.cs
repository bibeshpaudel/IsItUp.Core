namespace IsItUp.Domain.Entities;

public class ExternalLogin
{
    public string Provider { get; private set; }      // "Google", "GitHub"
    public string ProviderKey { get; private set; }
    public Guid UserId { get; private set; }
}