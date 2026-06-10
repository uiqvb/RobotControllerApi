using System.Security.Cryptography;
using System.Text;

namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Services;

public sealed class HmacSha256DeviceCredentialSecretHasher
{
    public const string Algorithm = "HMACSHA256_V1";
    public const string LegacyAlgorithm = "HMACSHA256";

    public string HashSecret(string rawSecret, string hashKey)
    {
        if (string.IsNullOrWhiteSpace(rawSecret))
        {
            throw new ArgumentException("Device credential secret cannot be empty.", nameof(rawSecret));
        }

        if (string.IsNullOrWhiteSpace(hashKey))
        {
            throw new InvalidOperationException("DeviceCredential:HashKey is not configured.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(hashKey);
        var secretBytes = Encoding.UTF8.GetBytes(rawSecret);

        try
        {
            using var hmac = new HMACSHA256(keyBytes);
            return Convert.ToBase64String(hmac.ComputeHash(secretBytes));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(keyBytes);
            CryptographicOperations.ZeroMemory(secretBytes);
        }
    }

    public bool VerifySecret(string rawSecret, string storedHash, string hashKey)
    {
        if (string.IsNullOrWhiteSpace(rawSecret) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        try
        {
            var actualHash = Convert.FromBase64String(HashSecret(rawSecret, hashKey));
            var expectedHash = Convert.FromBase64String(storedHash);
            return actualHash.Length == expectedHash.Length
                && CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
