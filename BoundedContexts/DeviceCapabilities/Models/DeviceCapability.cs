namespace RobotControllerApi.BoundedContexts.DeviceCapabilities.Models;

public class DeviceCapability
{
    public int Id { get; set; } = 0;
    public int DeviceId { get; set; } = 0;
    public int CommandCatalogueId { get; set; } = 0;
    public bool RequiresMap { get; set; } = false;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
