using System.Text.Json;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Models;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Persistence;
using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.Shared;
using RobotControllerApi.BoundedContexts.Telemetry.Dtos;
using RobotControllerApi.BoundedContexts.Telemetry.Models;
using RobotControllerApi.BoundedContexts.Telemetry.Persistence;

namespace RobotControllerApi.BoundedContexts.Telemetry.Services;

public class TelemetryService : ITelemetryService
{
    private const int DefaultLimit = 100;
    private const int MaxLimit = 1000;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);

    private readonly ITelemetryReadingDataAccess _dataAccess;
    private readonly IDeviceDataAccess _deviceDataAccess;
    private readonly IDeviceStatusDataAccess _deviceStatusDataAccess;
    private readonly IConditionClassifier _conditionClassifier;

    public TelemetryService(
        ITelemetryReadingDataAccess dataAccess,
        IDeviceDataAccess deviceDataAccess,
        IDeviceStatusDataAccess deviceStatusDataAccess,
        IConditionClassifier conditionClassifier)
    {
        _dataAccess = dataAccess;
        _deviceDataAccess = deviceDataAccess;
        _deviceStatusDataAccess = deviceStatusDataAccess;
        _conditionClassifier = conditionClassifier;
    }

    public TelemetryReadingResponse CreateTelemetryReading(int deviceId, CreateTelemetryReadingRequest request)
    {
        ValidateDevice(deviceId);
        ValidateProviderType(request.ProviderType);
        var payloadJson = GetPayloadJson(request);

        var now = DateTime.UtcNow;
        var model = new TelemetryReading
        {
            DeviceId = deviceId,
            PayloadJson = payloadJson,
            ProviderType = request.ProviderType.Trim(),
            RecordedAtUtc = request.RecordedAtUtc ?? now,
            CreatedDate = now
        };

        var created = _dataAccess.InsertTelemetryReading(model);
        UpdateDeviceStatus(deviceId, payloadJson, now);

        return MapToResponse(created);
    }

    public TelemetryReadingResponse? GetLatestTelemetryReadingByDeviceId(int deviceId)
    {
        ValidateDeviceId(deviceId);
        var model = _dataAccess.GetLatestTelemetryReadingByDeviceId(deviceId);
        return model == null ? null : MapToResponse(model);
    }

    public List<TelemetryReadingResponse> GetTelemetryReadingsByDeviceId(int deviceId, DateTime? fromUtc = null, DateTime? toUtc = null, int limit = DefaultLimit)
    {
        ValidateDeviceId(deviceId);

        if (fromUtc.HasValue && toUtc.HasValue && fromUtc.Value > toUtc.Value)
        {
            throw new ArgumentException("fromUtc cannot be later than toUtc.");
        }

        var safeLimit = NormalizeLimit(limit);

        return _dataAccess.GetTelemetryReadingsByDeviceId(deviceId, fromUtc, toUtc, safeLimit)
            .Select(MapToResponse)
            .ToList();
    }

    public LatestTelemetryResponse GetLatestTelemetrySummaryByDeviceId(int deviceId)
    {
        ValidateDeviceId(deviceId);

        var latest = _dataAccess.GetLatestTelemetryReadingByDeviceId(deviceId);
        if (latest == null)
        {
            return new LatestTelemetryResponse
            {
                DeviceId = deviceId,
                ConnectionState = "Unknown",
                OperationalState = "Unknown",
                StatusMessage = "No telemetry received.",
                IsStale = true,
                LastSeenAtUtc = null
            };
        }

        var now = DateTime.UtcNow;
        var isStale = now - latest.RecordedAtUtc > StaleAfter;
        var latestResponse = MapToResponse(latest);

        if (isStale)
        {
            return new LatestTelemetryResponse
            {
                DeviceId = deviceId,
                LatestReading = latestResponse,
                ConnectionState = "Offline",
                OperationalState = SafeExtractOperationalState(latest.PayloadJson),
                StatusMessage = "Telemetry is stale.",
                IsStale = true,
                LastSeenAtUtc = latest.RecordedAtUtc
            };
        }

        return new LatestTelemetryResponse
        {
            DeviceId = deviceId,
            LatestReading = latestResponse,
            ConnectionState = "Online",
            OperationalState = SafeExtractOperationalState(latest.PayloadJson),
            StatusMessage = SafeClassifyStatusMessage(latest.PayloadJson),
            IsStale = false,
            LastSeenAtUtc = latest.RecordedAtUtc
        };
    }

    private void UpdateDeviceStatus(int deviceId, string payloadJson, DateTime now)
    {
        var status = _deviceStatusDataAccess.GetDeviceStatusByDeviceId(deviceId);

        if (status == null)
        {
            _deviceStatusDataAccess.InsertDeviceStatus(new DeviceStatus
            {
                DeviceId = deviceId,
                ConnectionState = "Online",
                OperationalState = Truncate(SafeExtractOperationalState(payloadJson), 50),
                LastSeenAtUtc = now,
                LastHeartbeatAtUtc = now,
                StatusMessage = Truncate(SafeClassifyStatusMessage(payloadJson), 1000),
                CreatedDate = now,
                ModifiedDate = now
            });

            return;
        }

        status.ConnectionState = "Online";
        status.OperationalState = Truncate(SafeExtractOperationalState(payloadJson), 50);
        status.LastSeenAtUtc = now;
        status.LastHeartbeatAtUtc = now;
        status.StatusMessage = Truncate(SafeClassifyStatusMessage(payloadJson), 1000);
        status.ModifiedDate = now;

        _deviceStatusDataAccess.UpdateDeviceStatus(status.Id, status);
    }

    private void ValidateDevice(int deviceId)
    {
        ValidateDeviceId(deviceId);

        var device = _deviceDataAccess.GetDeviceById(deviceId);
        if (device == null || !device.IsActive)
        {
            throw new ArgumentException("Device must exist and be active.");
        }
    }

    private static void ValidateDeviceId(int deviceId)
    {
        if (deviceId <= 0)
        {
            throw new ArgumentException("DeviceId must be greater than zero.");
        }
    }

    private static void ValidateProviderType(string providerType)
    {
        if (string.IsNullOrWhiteSpace(providerType))
        {
            throw new ArgumentException("ProviderType is required.");
        }

        if (!DomainConstants.IsProviderType(providerType.Trim()))
        {
            throw new ArgumentException("ProviderType must be Api, Console, File, Poll, or Rollback.");
        }
    }

    private static string GetPayloadJson(CreateTelemetryReadingRequest request)
    {
        if (request.Payload.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Payload must be a JSON object.");
        }

        var rawJson = request.Payload.GetRawText();

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Payload must be a JSON object.");
            }
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Payload must be valid JSON.", ex);
        }

        return string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson;
    }

    private static int NormalizeLimit(int limit)
    {
        if (limit <= 0) return DefaultLimit;
        return limit > MaxLimit ? MaxLimit : limit;
    }

    private static string Truncate(string value, int maxLength)
    {
        if (value.Length <= maxLength) return value;
        return value[..maxLength];
    }

    private string SafeClassifyStatusMessage(string payloadJson)
    {
        try
        {
            return _conditionClassifier.ClassifyStatusMessage(payloadJson);
        }
        catch (JsonException)
        {
            return "Telemetry received.";
        }
    }

    private string SafeExtractOperationalState(string payloadJson)
    {
        try
        {
            return _conditionClassifier.ExtractOperationalState(payloadJson);
        }
        catch (JsonException)
        {
            return "Unknown";
        }
    }

    private static TelemetryReadingResponse MapToResponse(TelemetryReading model)
    {
        return new TelemetryReadingResponse
        {
            Id = model.Id,
            DeviceId = model.DeviceId,
            PayloadJson = model.PayloadJson,
            ProviderType = model.ProviderType,
            RecordedAtUtc = model.RecordedAtUtc,
            CreatedDate = model.CreatedDate
        };
    }
}
