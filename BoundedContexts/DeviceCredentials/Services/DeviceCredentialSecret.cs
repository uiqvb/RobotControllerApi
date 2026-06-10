namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Services;

public sealed class DeviceCredentialSecret
{
    public string RawSecret { get; init; } = string.Empty;
    public string SecretKeyPrefix { get; init; } = string.Empty;
    public string SecretHash { get; init; } = string.Empty;
    public string HashAlgorithm { get; init; } = string.Empty;
}
