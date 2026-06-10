using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Persistence;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Services;
using RobotControllerApi.BoundedContexts.Devices.Persistence;

namespace RobotControllerApi.BoundedContexts.Auth.Handlers;

public class DeviceCredentialAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string CredentialIdHeader = "X-Device-Credential-Id";
    private const string CredentialSecretHeader = "X-Device-Credential-Secret";

    private readonly IDeviceCredentialDataAccess _credentials;
    private readonly IDeviceDataAccess _devices;
    private readonly MultiDeviceCredentialSecretHashService _secretHasher;

    public DeviceCredentialAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IDeviceCredentialDataAccess credentials,
        IDeviceDataAccess devices,
        MultiDeviceCredentialSecretHashService secretHasher)
        : base(options, logger, encoder)
    {
        _credentials = credentials;
        _devices = devices;
        _secretHasher = secretHasher;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        Response.Headers["WWW-Authenticate"] = "DeviceCredential";

        if (!Request.Headers.TryGetValue(CredentialIdHeader, out var credentialIdentifierValues)
            || !Request.Headers.TryGetValue(CredentialSecretHeader, out var secretValues))
        {
            return FailAuthentication();
        }

        var credentialIdentifier = credentialIdentifierValues.ToString().Trim();
        var rawSecret = secretValues.ToString();

        if (string.IsNullOrWhiteSpace(credentialIdentifier) || string.IsNullOrWhiteSpace(rawSecret))
        {
            return FailAuthentication();
        }

        var credential = _credentials.GetDeviceCredentials()
            .FirstOrDefault(x => string.Equals(x.CredentialIdentifier, credentialIdentifier, StringComparison.OrdinalIgnoreCase));

        if (credential == null)
        {
            return FailAuthentication();
        }

        if (!credential.IsActive || credential.RevokedAtUtc.HasValue)
        {
            return FailAuthentication();
        }

        if (credential.ExpiresAtUtc.HasValue && credential.ExpiresAtUtc.Value <= DateTime.UtcNow)
        {
            return FailAuthentication();
        }

        if (!_secretHasher.VerifySecret(rawSecret, credential.SecretHash, credential.HashAlgorithm))
        {
            return FailAuthentication();
        }

        var device = _devices.GetDeviceById(credential.DeviceId);

        if (device == null || !device.IsActive)
        {
            return FailAuthentication();
        }

        if (RouteHasDeviceId(out var routeDeviceId) && routeDeviceId != credential.DeviceId)
        {
            return FailAuthentication();
        }

        try
        {
            credential.LastUsedAtUtc = DateTime.UtcNow;
            credential.LastUsedIpAddress = Context.Connection.RemoteIpAddress?.ToString();
            credential.LastUsedUserAgent = Request.Headers.UserAgent.ToString();
            credential.ModifiedDate = DateTime.UtcNow;
            _credentials.UpdateDeviceCredential(credential.Id, credential);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to update last-used metadata for device credential {CredentialId}.", credential.Id);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, credential.Id.ToString()),
            new(AuthClaimTypes.ActorType, AuthClaimTypes.DeviceAdapterActor),
            new(AuthClaimTypes.DeviceCredentialId, credential.Id.ToString()),
            new(AuthClaimTypes.DeviceId, credential.DeviceId.ToString()),
            new(AuthClaimTypes.CredentialIdentifier, credential.CredentialIdentifier),
            new(ClaimTypes.Name, credential.Name)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private bool RouteHasDeviceId(out int deviceId)
    {
        deviceId = 0;

        var routeValue = Context.Request.RouteValues["deviceId"]?.ToString();
        return int.TryParse(routeValue, out deviceId) && deviceId > 0;
    }

    private Task<AuthenticateResult> FailAuthentication()
    {
        return Task.FromResult(AuthenticateResult.Fail("Device credential authentication failed."));
    }
}
