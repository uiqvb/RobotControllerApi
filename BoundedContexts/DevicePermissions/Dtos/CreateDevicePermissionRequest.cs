namespace RobotControllerApi.BoundedContexts.DevicePermissions.Dtos;

public class CreateDevicePermissionRequest
{
    public int AppUserId { get; set; }
    public int DeviceId { get; set; }
    public string PermissionLevel { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAtUtc { get; set; } = null;
}
