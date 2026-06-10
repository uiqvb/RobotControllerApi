using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Dtos;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Models;
using RobotControllerApi.BoundedContexts.DeviceCredentials.Persistence;

namespace RobotControllerApi.BoundedContexts.DeviceCredentials.Services;

public class DeviceCredentialService : IDeviceCredentialService
{
    private readonly IDeviceCredentialDataAccess _dataAccess;
    private readonly IDeviceDataAccess _deviceDataAccess;
    private readonly MultiDeviceCredentialSecretHashService _secretHashService;

    public DeviceCredentialService(
        IDeviceCredentialDataAccess dataAccess,
        IDeviceDataAccess deviceDataAccess,
        MultiDeviceCredentialSecretHashService secretHashService)
    {
        _dataAccess = dataAccess;
        _deviceDataAccess = deviceDataAccess;
        _secretHashService = secretHashService;
    }

    public List<DeviceCredentialResponse> GetDeviceCredentials()
    {
        return _dataAccess.GetDeviceCredentials().Select(MapToResponse).ToList();
    }

    public DeviceCredentialResponse? GetDeviceCredentialById(int id)
    {
        var model = _dataAccess.GetDeviceCredentialById(id);
        return model == null ? null : MapToResponse(model);
    }

    public List<DeviceCredentialResponse> GetDeviceCredentialsByDeviceId(int deviceId)
    {
        return _dataAccess.GetDeviceCredentialsByDeviceId(deviceId).Select(MapToResponse).ToList();
    }

    public CreateDeviceCredentialResponse CreateDeviceCredential(int deviceId, CreateDeviceCredentialRequest request)
    {
        var device = _deviceDataAccess.GetDeviceById(deviceId);
        if (device == null || !device.IsActive) throw new ArgumentException("Device must exist and be active.");
        if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name is required.");
        if (request.Name.Length > 200) throw new ArgumentException("Name cannot exceed 200 characters.");
        if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc.Value <= DateTime.UtcNow) throw new ArgumentException("ExpiresAtUtc must be in the future.");

        var secret = _secretHashService.CreateSecret();
        var now = DateTime.UtcNow;
        var credentialIdentifier = GenerateCredentialIdentifier();

        while (_dataAccess.DeviceCredentialExistsByCredentialIdentifier(credentialIdentifier))
        {
            credentialIdentifier = GenerateCredentialIdentifier();
        }

        var model = new DeviceCredential
        {
            DeviceId = deviceId,
            Name = request.Name.Trim(),
            CredentialIdentifier = credentialIdentifier,
            SecretKeyPrefix = secret.SecretKeyPrefix,
            SecretHash = secret.SecretHash,
            HashAlgorithm = secret.HashAlgorithm,
            ExpiresAtUtc = request.ExpiresAtUtc,
            IsActive = true,
            CreatedDate = now,
            ModifiedDate = now
        };

        var created = _dataAccess.InsertDeviceCredential(model);
        var response = MapToCreateResponse(created);
        response.Secret = secret.RawSecret;
        return response;
    }

    public bool DeactivateDeviceCredential(int id)
    {
        var existing = _dataAccess.GetDeviceCredentialById(id);
        if (existing == null) return false;
        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceCredential(id, existing);
    }

    public bool ReactivateDeviceCredential(int id)
    {
        var existing = _dataAccess.GetDeviceCredentialById(id);
        if (existing == null) return false;
        if (existing.RevokedAtUtc.HasValue) throw new InvalidOperationException("Revoked credentials cannot be reactivated.");
        existing.IsActive = true;
        existing.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceCredential(id, existing);
    }

    public bool RevokeDeviceCredential(int id, RevokeDeviceCredentialRequest request)
    {
        var existing = _dataAccess.GetDeviceCredentialById(id);
        if (existing == null) return false;
        existing.IsActive = false;
        existing.RevokedAtUtc = DateTime.UtcNow;
        existing.RevocationReason = request.Reason;
        existing.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateDeviceCredential(id, existing);
    }

    public bool DeleteDeviceCredential(int id)
    {
        if (_dataAccess.GetDeviceCredentialById(id) == null) return false;
        return _dataAccess.DeleteDeviceCredential(id);
    }

    private static string GenerateCredentialIdentifier()
    {
        return "cred_" + Guid.NewGuid().ToString("N");
    }

    private static DeviceCredentialResponse MapToResponse(DeviceCredential model)
    {
        return new DeviceCredentialResponse
        {
            Id = model.Id,
            DeviceId = model.DeviceId,
            Name = model.Name,
            CredentialIdentifier = model.CredentialIdentifier,
            SecretKeyPrefix = model.SecretKeyPrefix,
            HashAlgorithm = model.HashAlgorithm,
            ExpiresAtUtc = model.ExpiresAtUtc,
            LastUsedAtUtc = model.LastUsedAtUtc,
            LastUsedIpAddress = model.LastUsedIpAddress,
            LastUsedUserAgent = model.LastUsedUserAgent,
            RevokedAtUtc = model.RevokedAtUtc,
            RevocationReason = model.RevocationReason,
            IsActive = model.IsActive,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }

    private static CreateDeviceCredentialResponse MapToCreateResponse(DeviceCredential model)
    {
        return new CreateDeviceCredentialResponse
        {
            Id = model.Id,
            DeviceId = model.DeviceId,
            Name = model.Name,
            CredentialIdentifier = model.CredentialIdentifier,
            SecretKeyPrefix = model.SecretKeyPrefix,
            HashAlgorithm = model.HashAlgorithm,
            ExpiresAtUtc = model.ExpiresAtUtc,
            LastUsedAtUtc = model.LastUsedAtUtc,
            LastUsedIpAddress = model.LastUsedIpAddress,
            LastUsedUserAgent = model.LastUsedUserAgent,
            RevokedAtUtc = model.RevokedAtUtc,
            RevocationReason = model.RevocationReason,
            IsActive = model.IsActive,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate,
            Secret = string.Empty
        };
    }
}
