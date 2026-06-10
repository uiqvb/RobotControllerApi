namespace RobotControllerApi.BoundedContexts.Devices.Dtos;

public class CreateDeviceRequest
{
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public int? MapId { get; set; } = null;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
}
