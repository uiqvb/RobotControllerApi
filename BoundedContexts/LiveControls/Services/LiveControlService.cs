using RobotControllerApi.BoundedContexts.Devices.Persistence;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Persistence;
using RobotControllerApi.BoundedContexts.Jobs.Services;
using RobotControllerApi.BoundedContexts.LiveControls.Dtos;
using RobotControllerApi.BoundedContexts.LiveControls.Models;
using RobotControllerApi.BoundedContexts.LiveControls.Persistence;
using RobotControllerApi.BoundedContexts.Shared;

namespace RobotControllerApi.BoundedContexts.LiveControls.Services;

public class LiveControlService : ILiveControlService
{
    private const int MinExpiresInMs = 100;
    private const int MaxExpiresInMs = 5000;
    private const int MaxSegmentDurationMs = 120000;

    private readonly ILiveControlCommandDataAccess _commandDataAccess;
    private readonly ILiveControlSessionDataAccess _sessionDataAccess;
    private readonly ILiveControlSegmentDataAccess _segmentDataAccess;
    private readonly IDeviceDataAccess _deviceDataAccess;
    private readonly IDeviceStatusDataAccess _deviceStatusDataAccess;

    public LiveControlService(
        ILiveControlCommandDataAccess commandDataAccess,
        ILiveControlSessionDataAccess sessionDataAccess,
        ILiveControlSegmentDataAccess segmentDataAccess,
        IDeviceDataAccess deviceDataAccess,
        IDeviceStatusDataAccess deviceStatusDataAccess)
    {
        _commandDataAccess = commandDataAccess;
        _sessionDataAccess = sessionDataAccess;
        _segmentDataAccess = segmentDataAccess;
        _deviceDataAccess = deviceDataAccess;
        _deviceStatusDataAccess = deviceStatusDataAccess;
    }

    public LiveControlSessionResponse StartLiveControlSession(int deviceId, StartLiveControlSessionRequest request, int appUserId)
    {
        ValidateDevice(deviceId);
        if (appUserId <= 0) throw new ArgumentException("AppUserId is required.");

        var active = _sessionDataAccess.GetActiveLiveControlSessionByDeviceId(deviceId);
        if (active != null)
        {
            active.Status = "Expired";
            active.StopReason = "NewSessionStarted";
            active.EndedAtUtc = DateTime.UtcNow;
            active.ModifiedDate = DateTime.UtcNow;
            _sessionDataAccess.UpdateLiveControlSession(active.Id, active);
        }

        if (request.RollbackOfLiveControlSessionId.HasValue && _sessionDataAccess.GetLiveControlSessionById(request.RollbackOfLiveControlSessionId.Value) == null)
        {
            throw new ArgumentException("RollbackOfLiveControlSessionId must reference an existing live control session.");
        }

        var now = DateTime.UtcNow;
        var session = _sessionDataAccess.InsertLiveControlSession(new LiveControlSession
        {
            DeviceId = deviceId,
            AppUserId = appUserId,
            Status = "Active",
            StopReason = null,
            IsRollback = request.IsRollback,
            RollbackOfLiveControlSessionId = request.RollbackOfLiveControlSessionId,
            StartedAtUtc = now,
            EndedAtUtc = null,
            CreatedDate = now,
            ModifiedDate = now
        });

        return MapSessionToResponse(session);
    }

    public bool StopLiveControlSession(int deviceId, int sessionId, StopLiveControlSessionRequest request, int appUserId)
    {
        ValidateDevice(deviceId);
        if (appUserId <= 0) throw new ArgumentException("AppUserId is required.");
        var session = _sessionDataAccess.GetLiveControlSessionById(sessionId);
        if (session == null) return false;
        if (session.DeviceId != deviceId) throw new InvalidOperationException("Live control session does not belong to the route device.");
        if (session.AppUserId != appUserId) throw new UnauthorizedAccessException("Only the user who started this live control session can stop it.");
        if (!DomainConstants.IsLiveControlStopReason(request.StopReason)) throw new ArgumentException("StopReason is invalid.");

        if (session.Status is "Completed" or "Cancelled" or "Failed" or "Expired") return true;

        var now = DateTime.UtcNow;
        session.Status = request.StopReason.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ? "Cancelled" : "Completed";
        session.StopReason = request.StopReason;
        session.EndedAtUtc = now;
        session.ModifiedDate = now;
        _sessionDataAccess.UpdateLiveControlSession(session.Id, session);

        SetStopCommand(deviceId, session.Id, appUserId, now);

        return true;
    }

    public List<LiveControlSessionResponse> GetLiveControlSessionsByDeviceId(int deviceId)
    {
        ValidateDeviceId(deviceId);
        return _sessionDataAccess.GetLiveControlSessionsByDeviceId(deviceId).Select(MapSessionToResponse).ToList();
    }

    public LiveControlSessionResponse? GetLiveControlSessionById(int id)
    {
        var session = _sessionDataAccess.GetLiveControlSessionById(id);
        return session == null ? null : MapSessionToResponse(session);
    }

    public LiveControlCommandResponse SetLiveControlCommand(int deviceId, SetLiveControlCommandRequest request, int appUserId)
    {
        ValidateDevice(deviceId);
        if (appUserId <= 0) throw new ArgumentException("AppUserId is required.");
        var commandName = NormalizeCommandName(request.CommandName);
        if (!DomainConstants.IsLiveControlCommand(commandName)) throw new ArgumentException("CommandName must be MOVE_FORWARD, MOVE_BACKWARD, ROTATE_LEFT, ROTATE_RIGHT, or STOP.");
        ValidateJsonObject(request.PayloadJson, "PayloadJson");
        if (request.ExpiresInMs < MinExpiresInMs || request.ExpiresInMs > MaxExpiresInMs) throw new ArgumentException($"ExpiresInMs must be between {MinExpiresInMs} and {MaxExpiresInMs}.");

        if (request.LiveControlSessionId.HasValue)
        {
            var session = _sessionDataAccess.GetLiveControlSessionById(request.LiveControlSessionId.Value);
            if (session == null) throw new ArgumentException("LiveControlSessionId must reference an existing live control session.");
            if (session.DeviceId != deviceId) throw new InvalidOperationException("Live control session does not belong to the route device.");
            if (session.AppUserId != appUserId) throw new UnauthorizedAccessException("Only the user who started this live control session can control it.");
            if (!session.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Live control session is not active.");
        }

        var now = DateTime.UtcNow;
        var existing = _commandDataAccess.GetLiveControlCommandByDeviceId(deviceId);

        if (existing == null)
        {
            existing = _commandDataAccess.InsertLiveControlCommand(new LiveControlCommand
            {
                DeviceId = deviceId,
                AppUserId = appUserId,
                LiveControlSessionId = request.LiveControlSessionId,
                CommandName = commandName,
                PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson,
                SequenceNumber = request.SequenceNumber,
                ExpiresAtUtc = now.AddMilliseconds(request.ExpiresInMs),
                CreatedDate = now,
                ModifiedDate = now
            });

            InvalidateGridPoseForMovementCommand(deviceId, commandName);
            return MapCommandToResponse(existing);
        }

        existing.AppUserId = appUserId;
        existing.LiveControlSessionId = request.LiveControlSessionId;
        existing.CommandName = commandName;
        existing.PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson;
        existing.SequenceNumber = request.SequenceNumber;
        existing.ExpiresAtUtc = now.AddMilliseconds(request.ExpiresInMs);
        existing.ModifiedDate = now;
        _commandDataAccess.UpdateLiveControlCommand(existing.Id, existing);
        InvalidateGridPoseForMovementCommand(deviceId, commandName);

        return MapCommandToResponse(existing);
    }

    public LiveControlCommandResponse GetLatestLiveControlCommandForAdapter(int deviceId, int authenticatedDeviceId)
    {
        ValidateDevice(deviceId);
        if (authenticatedDeviceId != deviceId) throw new UnauthorizedAccessException("Device credential does not own the route device.");

        var command = _commandDataAccess.GetLiveControlCommandByDeviceId(deviceId);
        if (command == null || command.ExpiresAtUtc <= DateTime.UtcNow)
        {
            return new LiveControlCommandResponse
            {
                Id = command?.Id ?? 0,
                DeviceId = deviceId,
                AppUserId = command?.AppUserId,
                LiveControlSessionId = command?.LiveControlSessionId,
                CommandName = "STOP",
                PayloadJson = "{}",
                SequenceNumber = command?.SequenceNumber ?? 0,
                ExpiresAtUtc = DateTime.UtcNow,
                IsExpired = true,
                CreatedDate = command?.CreatedDate ?? DateTime.UtcNow,
                ModifiedDate = command?.ModifiedDate ?? DateTime.UtcNow
            };
        }

        return MapCommandToResponse(command);
    }

    public LiveControlSegmentResponse CreateLiveControlSegment(int deviceId, int sessionId, CreateLiveControlSegmentRequest request, int authenticatedDeviceId)
    {
        ValidateDevice(deviceId);
        if (authenticatedDeviceId != deviceId) throw new UnauthorizedAccessException("Device credential does not own the route device.");
        var session = _sessionDataAccess.GetLiveControlSessionById(sessionId);
        if (session == null) throw new ArgumentException("Live control session does not exist.");
        if (session.DeviceId != deviceId) throw new InvalidOperationException("Live control session does not belong to the route device.");

        var commandName = NormalizeCommandName(request.CommandName);
        if (!DomainConstants.IsLiveControlCommand(commandName) || commandName == "STOP") throw new ArgumentException("CommandName must be a movement live-control command.");
        ValidateJsonObject(request.PayloadJson, "PayloadJson");
        if (request.DurationMs <= 0 || request.DurationMs > MaxSegmentDurationMs) throw new ArgumentException($"DurationMs must be between 1 and {MaxSegmentDurationMs}.");
        if (!request.Success && string.IsNullOrWhiteSpace(request.FailureCode)) throw new ArgumentException("FailureCode is required when Success is false.");

        var now = DateTime.UtcNow;
        var completed = request.CompletedAtUtc ?? now;
        var started = request.StartedAtUtc ?? completed.AddMilliseconds(-request.DurationMs);
        if (started > completed) throw new ArgumentException("StartedAtUtc cannot be later than CompletedAtUtc.");

        var segment = _segmentDataAccess.InsertLiveControlSegment(new LiveControlSegment
        {
            LiveControlSessionId = sessionId,
            CommandName = commandName,
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson,
            DurationMs = request.DurationMs,
            Success = request.Success,
            FailureCode = request.FailureCode,
            FailureMessage = request.FailureMessage,
            StartedAtUtc = started,
            CompletedAtUtc = completed,
            CreatedDate = now
        });

        return MapSegmentToResponse(segment);
    }

    public List<LiveControlSegmentResponse> GetLiveControlSegmentsBySessionId(int sessionId)
    {
        if (_sessionDataAccess.GetLiveControlSessionById(sessionId) == null) throw new ArgumentException("Live control session does not exist.");
        return _segmentDataAccess.GetLiveControlSegmentsBySessionId(sessionId).Select(MapSegmentToResponse).ToList();
    }

    private void SetStopCommand(int deviceId, int sessionId, int appUserId, DateTime now)
    {
        var existing = _commandDataAccess.GetLiveControlCommandByDeviceId(deviceId);
        if (existing == null)
        {
            _commandDataAccess.InsertLiveControlCommand(new LiveControlCommand
            {
                DeviceId = deviceId,
                AppUserId = appUserId,
                LiveControlSessionId = sessionId,
                CommandName = "STOP",
                PayloadJson = "{}",
                SequenceNumber = 0,
                ExpiresAtUtc = now.AddMilliseconds(MaxExpiresInMs),
                CreatedDate = now,
                ModifiedDate = now
            });
            return;
        }

        existing.AppUserId = appUserId;
        existing.LiveControlSessionId = sessionId;
        existing.CommandName = "STOP";
        existing.PayloadJson = "{}";
        existing.ExpiresAtUtc = now.AddMilliseconds(MaxExpiresInMs);
        existing.ModifiedDate = now;
        _commandDataAccess.UpdateLiveControlCommand(existing.Id, existing);
    }


    private void InvalidateGridPoseForMovementCommand(int deviceId, string commandName)
    {
        if (commandName.Equals("STOP", StringComparison.OrdinalIgnoreCase)) return;

        var status = _deviceStatusDataAccess.GetDeviceStatusByDeviceId(deviceId);
        if (status == null) return;
        if (!status.IsGridPoseTrusted && !status.IsGridAligned) return;

        status.IsGridPoseTrusted = false;
        status.IsGridAligned = false;
        status.PoseConfidence = 0;
        status.StatusMessage = "Grid pose invalidated by live/free-hand control.";
        status.ModifiedDate = DateTime.UtcNow;
        _deviceStatusDataAccess.UpdateDeviceStatus(status.Id, status);
    }

    private void ValidateDevice(int deviceId)
    {
        ValidateDeviceId(deviceId);
        var device = _deviceDataAccess.GetDeviceById(deviceId);
        if (device == null || !device.IsActive) throw new ArgumentException("Device must exist and be active.");
    }

    private static void ValidateDeviceId(int deviceId)
    {
        if (deviceId <= 0) throw new ArgumentException("DeviceId must be greater than zero.");
    }

    private static string NormalizeCommandName(string commandName)
    {
        return string.IsNullOrWhiteSpace(commandName) ? string.Empty : commandName.Trim().ToUpperInvariant();
    }

    private static void ValidateJsonObject(string? json, string name)
    {
        JobService.ValidateJsonObject(json, name);
    }

    private static LiveControlCommandResponse MapCommandToResponse(LiveControlCommand model) => new()
    {
        Id = model.Id,
        DeviceId = model.DeviceId,
        AppUserId = model.AppUserId,
        LiveControlSessionId = model.LiveControlSessionId,
        CommandName = model.CommandName,
        PayloadJson = model.PayloadJson,
        SequenceNumber = model.SequenceNumber,
        ExpiresAtUtc = model.ExpiresAtUtc,
        IsExpired = model.ExpiresAtUtc <= DateTime.UtcNow,
        CreatedDate = model.CreatedDate,
        ModifiedDate = model.ModifiedDate
    };

    private static LiveControlSessionResponse MapSessionToResponse(LiveControlSession model) => new()
    {
        Id = model.Id,
        DeviceId = model.DeviceId,
        AppUserId = model.AppUserId,
        Status = model.Status,
        StopReason = model.StopReason,
        IsRollback = model.IsRollback,
        RollbackOfLiveControlSessionId = model.RollbackOfLiveControlSessionId,
        StartedAtUtc = model.StartedAtUtc,
        EndedAtUtc = model.EndedAtUtc,
        CreatedDate = model.CreatedDate,
        ModifiedDate = model.ModifiedDate
    };

    private static LiveControlSegmentResponse MapSegmentToResponse(LiveControlSegment model) => new()
    {
        Id = model.Id,
        LiveControlSessionId = model.LiveControlSessionId,
        CommandName = model.CommandName,
        PayloadJson = model.PayloadJson,
        DurationMs = model.DurationMs,
        Success = model.Success,
        FailureCode = model.FailureCode,
        FailureMessage = model.FailureMessage,
        StartedAtUtc = model.StartedAtUtc,
        CompletedAtUtc = model.CompletedAtUtc,
        CreatedDate = model.CreatedDate
    };
}
