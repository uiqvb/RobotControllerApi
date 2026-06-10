namespace RobotControllerApi.BoundedContexts.DeviceStatuses.Dtos;

public class UpdateDeviceStatusRequest
{
    public string ConnectionState { get; set; } = "Unknown";
    public string OperationalState { get; set; } = "Unknown";
    public string? StatusMessage { get; set; } = null;
    public string? LastErrorCode { get; set; } = null;
    public string? LastErrorMessage { get; set; } = null;
}
