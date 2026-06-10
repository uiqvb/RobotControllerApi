namespace RobotControllerApi.BoundedContexts.AppUsers.Models;

public class AppUser
{
    public int Id { get; set; } = 0;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAtUtc { get; set; } = null;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
