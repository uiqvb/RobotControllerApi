using System.Security.Claims;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;
using RobotControllerApi.BoundedContexts.DevicePermissions.Persistence;

namespace RobotControllerApi.BoundedContexts.Auth.Services;

public class DevicePermissionAuthorizationService
{
    private static readonly Dictionary<string, int> PermissionRanks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Viewer"] = 1,
        ["Operator"] = 2,
        ["Manager"] = 3,
        ["Owner"] = 4
    };

    private readonly IAppUserDataAccess _users;
    private readonly IDevicePermissionDataAccess _permissions;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public DevicePermissionAuthorizationService(
        IAppUserDataAccess users,
        IDevicePermissionDataAccess permissions,
        CurrentUserAccessor currentUserAccessor)
    {
        _users = users;
        _permissions = permissions;
        _currentUserAccessor = currentUserAccessor;
    }

    public bool IsAdmin(ClaimsPrincipal user)
    {
        var appUserId = _currentUserAccessor.GetRequiredAppUserId(user);
        var appUser = _users.GetAppUserById(appUserId);
        return appUser != null
            && appUser.IsActive
            && string.Equals(appUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    public bool CanViewDevice(ClaimsPrincipal user, int deviceId) => HasPermission(user, deviceId, "Viewer");
    public bool CanOperateDevice(ClaimsPrincipal user, int deviceId) => HasPermission(user, deviceId, "Operator");
    public bool CanManageDevice(ClaimsPrincipal user, int deviceId) => HasPermission(user, deviceId, "Manager");
    public bool CanOwnDevice(ClaimsPrincipal user, int deviceId) => HasPermission(user, deviceId, "Owner");

    private bool HasPermission(ClaimsPrincipal user, int deviceId, string requiredLevel)
    {
        var appUserId = _currentUserAccessor.GetRequiredAppUserId(user);
        var appUser = _users.GetAppUserById(appUserId);

        if (appUser == null || !appUser.IsActive)
        {
            return false;
        }

        if (string.Equals(appUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var permission = _permissions.GetDevicePermissionsByUserId(appUserId)
            .Where(x => x.DeviceId == deviceId)
            .Where(x => x.IsActive)
            .Where(x => !x.ExpiresAtUtc.HasValue || x.ExpiresAtUtc.Value > now)
            .OrderByDescending(x => GetRank(x.PermissionLevel))
            .FirstOrDefault();

        return permission != null && GetRank(permission.PermissionLevel) >= GetRank(requiredLevel);
    }

    private static int GetRank(string permissionLevel)
    {
        return PermissionRanks.TryGetValue(permissionLevel, out var rank) ? rank : 0;
    }
}
