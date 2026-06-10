namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Dtos;

public class CreateDeviceCredentialRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; } = null;
}
