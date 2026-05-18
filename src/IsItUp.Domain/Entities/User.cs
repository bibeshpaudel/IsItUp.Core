using IsItUp.Domain.Enums;

namespace IsItUp.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string NormalizedEmail { get; private set; }
    public string PasswordHash { get; private set; }
    public bool EmailConfirmed { get; private set; }
    public bool TwoFactorEnabled { get; private set; }
    public TwoFactorMethod TwoFactorMethod { get; private set; }  // Totp | Email | None
    public string? TotpSecret { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }
    public int AccessFailedCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<UserRole> Roles { get; }
    public IReadOnlyCollection<ExternalLogin> ExternalLogins { get; }
    public IReadOnlyCollection<PasskeyCredential> Passkeys { get; }
    public IReadOnlyCollection<RefreshToken> RefreshTokens { get; }
}