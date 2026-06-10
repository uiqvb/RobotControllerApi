using System.Security.Claims;
using RobotControllerApi.BoundedContexts.Auth.Constants;

namespace RobotControllerApi.BoundedContexts.Auth.Services;

public class CurrentUserAccessor
{
    public int GetRequiredAppUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.AppUserId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var appUserId) || appUserId <= 0)
        {
            throw new UnauthorizedAccessException("Authenticated human user id was not present in claims.");
        }

        return appUserId;
    }

    public int GetRequiredDeviceCredentialId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.DeviceCredentialId)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(value, out var credentialId) || credentialId <= 0)
        {
            throw new UnauthorizedAccessException("Authenticated device credential id was not present in claims.");
        }

        return credentialId;
    }

    public int GetRequiredDeviceId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AuthClaimTypes.DeviceId);

        if (!int.TryParse(value, out var deviceId) || deviceId <= 0)
        {
            throw new UnauthorizedAccessException("Authenticated device id was not present in claims.");
        }

        return deviceId;
    }
}
