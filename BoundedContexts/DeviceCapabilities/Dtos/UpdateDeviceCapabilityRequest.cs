namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Dtos;

public class UpdateDeviceCapabilityRequest
{
    public int DeviceId { get; set; }
    public int CommandCatalogueId { get; set; }
    public bool RequiresMap { get; set; } = false;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
}
