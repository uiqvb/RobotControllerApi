namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Dtos;

public class DeviceCapabilityResponse
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; } = 0;
    public int CommandCatalogueId { get; set; } = 0;
    public bool RequiresMap { get; set; } = false;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = default;
    public DateTime ModifiedDate { get; set; } = default;
}
