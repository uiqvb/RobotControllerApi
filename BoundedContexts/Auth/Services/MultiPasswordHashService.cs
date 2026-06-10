using System.Security.Cryptography;

namespace RobotControllerApi.BoundedContexts.Auth.Services;

public sealed class MultiPasswordHashService
{
    public const string CurrentAlgorithm = "PBKDF2_SHA256_V2";

    private const string LegacyAlgorithm = "PBKDF2_SHA256_V1";
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int V1Iterations = 100_000;
    private const int V2Iterations = 210_000;

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            V2Iterations,
            HashAlgorithmName.SHA256,
            HashSizeBytes);

        return string.Join('$',
            CurrentAlgorithm,
            V2Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (!TryParseHash(storedHash, out var algorithm, out var iterations, out var salt, out var expectedHash))
        {
            return false;
        }

        if (!IsSupportedAlgorithm(algorithm))
        {
            return false;
        }

        try
        {
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(string storedHash)
    {
        if (!TryParseHash(storedHash, out var algorithm, out var iterations, out _, out _))
        {
            return true;
        }

        return !string.Equals(algorithm, CurrentAlgorithm, StringComparison.Ordinal)
            || iterations < V2Iterations;
    }

    public string GetCurrentAlgorithm() => CurrentAlgorithm;

    public string GetAlgorithmFromHash(string storedHash)
    {
        return TryParseHash(storedHash, out var algorithm, out _, out _, out _)
            ? algorithm
            : "Unknown";
    }

    private static bool IsSupportedAlgorithm(string algorithm)
    {
        return string.Equals(algorithm, CurrentAlgorithm, StringComparison.Ordinal)
            || string.Equals(algorithm, LegacyAlgorithm, StringComparison.Ordinal);
    }

    private static bool TryParseHash(
        string storedHash,
        out string algorithm,
        out int iterations,
        out byte[] salt,
        out byte[] hash)
    {
        algorithm = string.Empty;
        iterations = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }

        algorithm = parts[0];

        if (!int.TryParse(parts[1], out iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
            return salt.Length > 0 && hash.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
