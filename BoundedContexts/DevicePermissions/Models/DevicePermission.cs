namespace RobotControllerApi.BoundedContexts.DevicePermissions.Models;

public class DevicePermission
{
    public int Id { get; set; } = 0;
    public int AppUserId { get; set; } = 0;
    public int DeviceId { get; set; } = 0;
    public string PermissionLevel { get; set; } = "Viewer";
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAtUtc { get; set; } = null;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
