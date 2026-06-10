namespace RobotControllerApi.BoundedContexts.Devices.Dtos;

public class DeviceResponse
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public int? MapId { get; set; } = null;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = default;
    public DateTime ModifiedDate { get; set; } = default;
}
