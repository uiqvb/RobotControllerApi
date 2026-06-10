namespace RobotControllerApi.BoundedContexts.Jobs.Persistence;

public class DeviceCapabilitySnapshot
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    public int CommandCatalogueId { get; set; }
    public bool RequiresMap { get; set; }
    public bool IsActive { get; set; }
}
