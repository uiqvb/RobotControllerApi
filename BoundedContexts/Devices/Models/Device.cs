namespace RobotControllerApi.BoundedContexts.Devices.Models;

public class Device
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public int? MapId { get; set; } = null;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
