namespace RobotControllerApi.BoundedContexts.Auth.Dtos;

public class LoginResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
