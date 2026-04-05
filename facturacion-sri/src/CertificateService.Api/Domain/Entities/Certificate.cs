namespace CertificateService.Api.Domain.Entities;

public sealed class Certificate
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public byte[] EncryptedP12 { get; private set; } = Array.Empty<byte>();
    public string EncryptedPassword { get; private set; } = string.Empty;
    public DateTime ExpirationDateUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    private Certificate() { }

    public Certificate(Guid tenantId, string name, byte[] encryptedP12, string encryptedPassword, DateTime expirationDateUtc, bool isActive)
    {
        TenantId = tenantId;
        Name = name;
        EncryptedP12 = encryptedP12;
        EncryptedPassword = encryptedPassword;
        ExpirationDateUtc = expirationDateUtc;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
