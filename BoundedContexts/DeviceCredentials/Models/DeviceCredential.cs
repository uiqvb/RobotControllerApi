namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Models;

public class DeviceCredential
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public string CredentialIdentifier { get; set; } = string.Empty;
    public string SecretKeyPrefix { get; set; } = string.Empty;
    public string SecretHash { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = "HMACSHA256_V1";
    public DateTime? ExpiresAtUtc { get; set; } = null;
    public DateTime? LastUsedAtUtc { get; set; } = null;
    public string? LastUsedIpAddress { get; set; } = null;
    public string? LastUsedUserAgent { get; set; } = null;
    public DateTime? RevokedAtUtc { get; set; } = null;
    public string? RevocationReason { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
