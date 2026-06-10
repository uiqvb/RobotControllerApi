using System.Security.Cryptography;

namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Services;

public sealed class MultiDeviceCredentialSecretHashService
{
    public const string CurrentAlgorithm = HmacSha256DeviceCredentialSecretHasher.Algorithm;

    private readonly IConfiguration _configuration;
    private readonly HmacSha256DeviceCredentialSecretHasher _hmacSha256 = new();

    public MultiDeviceCredentialSecretHashService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DeviceCredentialSecret CreateSecret()
    {
        var rawSecret = GenerateSecret();

        return new DeviceCredentialSecret
        {
            RawSecret = rawSecret,
            SecretKeyPrefix = GetSecretPrefix(rawSecret),
            SecretHash = HashSecret(rawSecret),
            HashAlgorithm = CurrentAlgorithm
        };
    }

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return "rc_live_" + ToBase64Url(bytes);
    }

    public string GetSecretPrefix(string rawSecret)
    {
        if (string.IsNullOrWhiteSpace(rawSecret))
        {
            return string.Empty;
        }

        return rawSecret.Length <= 16 ? rawSecret : rawSecret[..16];
    }

    public string HashSecret(string rawSecret)
    {
        return _hmacSha256.HashSecret(rawSecret, GetHashKey());
    }

    public bool VerifySecret(string rawSecret, string storedHash, string hashAlgorithm)
    {
        if (string.Equals(hashAlgorithm, HmacSha256DeviceCredentialSecretHasher.Algorithm, StringComparison.OrdinalIgnoreCase)
            || string.Equals(hashAlgorithm, HmacSha256DeviceCredentialSecretHasher.LegacyAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            return _hmacSha256.VerifySecret(rawSecret, storedHash, GetHashKey());
        }

        return false;
    }

    public bool NeedsRehash(string hashAlgorithm)
    {
        return !string.Equals(hashAlgorithm, CurrentAlgorithm, StringComparison.OrdinalIgnoreCase);
    }

    public string GetCurrentAlgorithm() => CurrentAlgorithm;

    private string GetHashKey()
    {
        return _configuration["DeviceCredential:HashKey"]
            ?? throw new InvalidOperationException("DeviceCredential:HashKey is not configured.");
    }

    private static string ToBase64Url(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
