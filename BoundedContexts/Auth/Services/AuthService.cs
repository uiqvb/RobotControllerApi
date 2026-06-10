using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;
using RobotControllerApi.BoundedContexts.Auth.Dtos;

namespace RobotControllerApi.BoundedContexts.Auth.Services;

public class AuthService
{
    private const int MaxFailedLoginCount = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IAppUserDataAccess _users;
    private readonly IAppUserCredentialDataAccess _credentials;
    private readonly MultiPasswordHashService _passwordHasher;

    public AuthService(
        IAppUserDataAccess users,
        IAppUserCredentialDataAccess credentials,
        MultiPasswordHashService passwordHasher)
    {
        _users = users;
        _credentials = credentials;
        _passwordHasher = passwordHasher;
    }

    public LoginResponse Register(RegisterRequest request)
    {
        ValidateRegistration(request);

        var email = request.Email.Trim();

        if (_users.AppUserExistsByEmail(email))
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        // Bootstrap rule:
        // - The very first user in an empty database becomes Admin.
        // - Every later anonymous registration becomes User.
        // The caller never chooses their own role.
        var role = _users.GetAppUsers().Any() ? "User" : "Admin";

        var now = DateTime.UtcNow;
        var user = _users.CreateAppUser(new AppUser
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Role = role,
            IsActive = true,
            LastLoginAtUtc = null,
            CreatedDate = now,
            ModifiedDate = now
        });

        var passwordHash = _passwordHasher.HashPassword(request.Password);

        _credentials.InsertCredential(new AppUserCredential
        {
            AppUserId = user.Id,
            PasswordHash = passwordHash,
            PasswordHashAlgorithm = _passwordHasher.GetCurrentAlgorithm(),
            PasswordChangedAtUtc = now,
            MustResetPassword = false,
            FailedLoginCount = 0,
            LockedOutUntilUtc = null,
            CreatedDate = now,
            ModifiedDate = now
        });

        return MapToLoginResponse(user);
    }

    public LoginResponse ValidateLogin(string email, string password)
    {
        var user = _users.GetAppUserByEmail(email.Trim());

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var credential = _credentials.GetCredentialByAppUserId(user.Id);

        if (credential == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (credential.LockedOutUntilUtc.HasValue && credential.LockedOutUntilUtc.Value > DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("User account is temporarily locked.");
        }

        if (!_passwordHasher.VerifyPassword(password, credential.PasswordHash))
        {
            RegisterFailedLogin(credential);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (credential.MustResetPassword)
        {
            throw new UnauthorizedAccessException("Password reset is required before login.");
        }

        RegisterSuccessfulLogin(user, credential, password);
        return MapToLoginResponse(user);
    }


    public LoginResponse ValidateBasicAuthentication(string email, string password)
    {
        var user = _users.GetAppUserByEmail(email.Trim());

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var credential = _credentials.GetCredentialByAppUserId(user.Id);

        if (credential == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (credential.LockedOutUntilUtc.HasValue && credential.LockedOutUntilUtc.Value > DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("User account is temporarily locked.");
        }

        if (!_passwordHasher.VerifyPassword(password, credential.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (credential.MustResetPassword)
        {
            throw new UnauthorizedAccessException("Password reset is required before login.");
        }

        return MapToLoginResponse(user);
    }

    public CurrentUserResponse GetCurrentUser(int appUserId)
    {
        var user = _users.GetAppUserById(appUserId)
            ?? throw new UnauthorizedAccessException("Current user was not found.");

        return new CurrentUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }

    public bool VerifyPassword(int appUserId, string password)
    {
        var user = _users.GetAppUserById(appUserId);
        if (user == null || !user.IsActive) return false;

        var credential = _credentials.GetCredentialByAppUserId(appUserId);
        return credential != null && _passwordHasher.VerifyPassword(password, credential.PasswordHash);
    }

    private void RegisterFailedLogin(AppUserCredential credential)
    {
        credential.FailedLoginCount += 1;
        credential.ModifiedDate = DateTime.UtcNow;

        if (credential.FailedLoginCount >= MaxFailedLoginCount)
        {
            credential.LockedOutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
        }

        _credentials.UpdateCredential(credential.Id, credential);
    }

    private void RegisterSuccessfulLogin(AppUser user, AppUserCredential credential, string plainTextPassword)
    {
        var now = DateTime.UtcNow;

        user.LastLoginAtUtc = now;
        user.ModifiedDate = now;
        _users.UpdateAppUser(user.Id, user);

        credential.FailedLoginCount = 0;
        credential.LockedOutUntilUtc = null;
        credential.ModifiedDate = now;

        if (_passwordHasher.NeedsRehash(credential.PasswordHash))
        {
            credential.PasswordHash = _passwordHasher.HashPassword(plainTextPassword);
            credential.PasswordHashAlgorithm = _passwordHasher.GetCurrentAlgorithm();
            credential.PasswordChangedAtUtc = now;
        }

        _credentials.UpdateCredential(credential.Id, credential);
    }

    private static void ValidateRegistration(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) throw new ArgumentException("Email is required.");
        if (request.Email.Length > 320) throw new ArgumentException("Email cannot exceed 320 characters.");
        if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ArgumentException("DisplayName is required.");
        if (request.DisplayName.Length > 200) throw new ArgumentException("DisplayName cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(request.Password)) throw new ArgumentException("Password is required.");
        if (request.Password.Length < 8) throw new ArgumentException("Password must be at least 8 characters.");
    }

    private static LoginResponse MapToLoginResponse(AppUser user)
    {
        return new LoginResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            LastLoginAtUtc = user.LastLoginAtUtc
        };
    }
}
