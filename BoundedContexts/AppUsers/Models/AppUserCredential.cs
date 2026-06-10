namespace RobotControllerApi.BoundedContexts.AppUsers.Models;

public class AppUserCredential
{
    public int Id { get; set; }
    public int AppUserId { get; set; }

    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordHashAlgorithm { get; set; } = "PBKDF2_SHA256_V2";

    public DateTime? PasswordChangedAtUtc { get; set; }
    public bool MustResetPassword { get; set; } = false;
    public int FailedLoginCount { get; set; }
    public DateTime? LockedOutUntilUtc { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
