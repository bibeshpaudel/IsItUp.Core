namespace IsItUp.Domain.Entities;

public class PasskeyCredential
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public byte[] CredentialId { get; private set; }
    public byte[] PublicKey { get; private set; }
    public uint SignCount { get; private set; }
    public string AaguidName { get; private set; }    // "YubiKey 5", "Touch ID", etc.
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
}
