namespace RobotControllerApi.BoundedContexts.AppUsers.Dtos;

public class CreateAppUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; } = true;
}
