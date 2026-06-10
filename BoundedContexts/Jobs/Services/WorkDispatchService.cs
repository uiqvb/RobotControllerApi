using System.Text.Json;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Models;
using RobotControllerApi.BoundedContexts.DeviceStatuses.Persistence;
using RobotControllerApi.BoundedContexts.JobHistories.Models;
using RobotControllerApi.BoundedContexts.JobHistories.Persistence;
using RobotControllerApi.BoundedContexts.Jobs.Dtos;
using RobotControllerApi.BoundedContexts.Jobs.Models;
using RobotControllerApi.BoundedContexts.Jobs.Persistence;
using RobotControllerApi.BoundedContexts.Maps.Models;
using RobotControllerApi.BoundedContexts.Maps.Persistence;
using RobotControllerApi.BoundedContexts.Shared;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Models;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Persistence;
using RobotControllerApi.BoundedContexts.Workflows.Models;
using RobotControllerApi.BoundedContexts.Workflows.Persistence;

namespace RobotControllerApi.BoundedContexts.Jobs.Services;

public class WorkDispatchService : IWorkDispatchService
{
    private readonly IJobDataAccess _jobDataAccess;
    private readonly IWorkflowDataAccess _workflowDataAccess;
    private readonly IJobHistoryDataAccess _jobHistoryDataAccess;
    private readonly IWorkflowHistoryDataAccess _workflowHistoryDataAccess;
    private readonly IDeviceStatusDataAccess _deviceStatusDataAccess;
    private readonly IMapDataAccess _mapDataAccess;
    private readonly IConfiguration _configuration;

    public WorkDispatchService(
        IJobDataAccess jobDataAccess,
        IWorkflowDataAccess workflowDataAccess,
        IJobHistoryDataAccess jobHistoryDataAccess,
        IWorkflowHistoryDataAccess workflowHistoryDataAccess,
        IDeviceStatusDataAccess deviceStatusDataAccess,
        IMapDataAccess mapDataAccess,
        IConfiguration configuration)
    {
        _jobDataAccess = jobDataAccess;
        _workflowDataAccess = workflowDataAccess;
        _jobHistoryDataAccess = jobHistoryDataAccess;
        _workflowHistoryDataAccess = workflowHistoryDataAccess;
        _deviceStatusDataAccess = deviceStatusDataAccess;
        _mapDataAccess = mapDataAccess;
        _configuration = configuration;
    }

    public WorkItemClaimResponse? ClaimNextWorkItem(int deviceId, ClaimJobRequest request, int deviceCredentialId)
    {
        if (deviceId <= 0) throw new ArgumentException("DeviceId is required.");
        if (deviceCredentialId <= 0) throw new ArgumentException("DeviceCredentialId is required.");
        if (!_jobDataAccess.DeviceCredentialOwnsDevice(deviceCredentialId, deviceId)) throw new InvalidOperationException("DeviceCredential does not own the route device.");

        var now = DateTime.UtcNow;
        var configuredLeaseMinutes = int.TryParse(_configuration["WorkDispatch:LeaseMinutes"], out var parsedLeaseMinutes) ? parsedLeaseMinutes : 5;
        var leaseMinutes = request.LeaseMinutes > 0 ? request.LeaseMinutes : configuredLeaseMinutes;

        ExpireStaleWork(deviceId, now);

        // A single claim call may skip several invalid BestEffort steps before it finds
        // a step that can be physically dispatched. The limit prevents an accidental
        // infinite loop if data is corrupt.
        for (var i = 0; i < 50; i++)
        {
            var job = _jobDataAccess.GetOldestQueuedJobByDeviceId(deviceId);
            if (job == null) return null;

            var decision = PrepareQueuedJobForDispatch(job, now);
            if (decision == DispatchDecision.SkipAndContinue)
            {
                continue;
            }

            if (decision == DispatchDecision.StopWithoutWork)
            {
                return null;
            }

            job.Status = "Claimed";
            job.ClaimedByDeviceCredentialId = deviceCredentialId;
            job.ClaimedAtUtc = now;
            job.LeaseExpiresAtUtc = now.AddMinutes(leaseMinutes);
            job.ModifiedDate = now;
            _jobDataAccess.UpdateJob(job.Id, job);

            MarkParentWorkflowClaimed(job.WorkflowId, deviceCredentialId, now, leaseMinutes);

            return new WorkItemClaimResponse { WorkItemType = "Job", Job = JobService.MapToResponse(job) };
        }

        return null;
    }

    private DispatchDecision PrepareQueuedJobForDispatch(Job job, DateTime now)
    {
        if (!job.WorkflowId.HasValue)
        {
            var standaloneValidation = ValidateJobAgainstCurrentPose(job);
            if (standaloneValidation.IsValid) return DispatchDecision.Claim;

            MarkJobValidationFailed(job, standaloneValidation.FailureCode, standaloneValidation.FailureMessage, false, now);
            return DispatchDecision.SkipAndContinue;
        }

        var workflow = _workflowDataAccess.GetWorkflowById(job.WorkflowId.Value);
        if (workflow == null)
        {
            MarkJobValidationFailed(job, "WORKFLOW_MISSING", "Parent workflow could not be found.", false, now);
            return DispatchDecision.SkipAndContinue;
        }

        if (IsTerminalStatus(workflow.Status))
        {
            job.Status = "Cancelled";
            job.ModifiedDate = now;
            _jobDataAccess.UpdateJob(job.Id, job);
            return DispatchDecision.SkipAndContinue;
        }

        if (workflow.ExecutionMode.Equals("AllOrNothing", StringComparison.OrdinalIgnoreCase))
        {
            var allOrNothingValidation = ValidateRemainingWorkflowSteps(workflow);
            if (!allOrNothingValidation.IsValid)
            {
                FailWorkflowBeforeDispatch(workflow, allOrNothingValidation.Job, allOrNothingValidation.FailureCode, allOrNothingValidation.FailureMessage, now);
                return DispatchDecision.SkipAndContinue;
            }

            return DispatchDecision.Claim;
        }

        var stepValidation = ValidateJobAgainstCurrentPose(job);
        if (stepValidation.IsValid) return DispatchDecision.Claim;

        MarkJobValidationFailed(job, stepValidation.FailureCode, stepValidation.FailureMessage, false, now);
        FinalizeParentWorkflowIfReady(workflow.Id, now);
        return DispatchDecision.SkipAndContinue;
    }

    private ValidationResult ValidateRemainingWorkflowSteps(Workflow workflow)
    {
        var queuedJobs = _jobDataAccess.GetJobsByWorkflowId(workflow.Id)
            .Where(x => x.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.StepNumber)
            .ThenBy(x => x.Id)
            .ToList();

        var pose = BuildCurrentPose(workflow.DeviceId);

        foreach (var queuedJob in queuedJobs)
        {
            var commandName = _jobDataAccess.GetCommandCatalogueNameById(queuedJob.CommandCatalogueId) ?? string.Empty;
            var validation = ValidateAndSimulate(commandName, queuedJob.PayloadJson, workflow.DeviceId, pose);
            if (!validation.IsValid)
            {
                validation.Job = queuedJob;
                return validation;
            }

            pose = validation.NextPose ?? pose;
        }

        return ValidationResult.Valid();
    }

    private ValidationResult ValidateJobAgainstCurrentPose(Job job)
    {
        var commandName = _jobDataAccess.GetCommandCatalogueNameById(job.CommandCatalogueId) ?? string.Empty;
        var pose = BuildCurrentPose(job.DeviceId);
        return ValidateAndSimulate(commandName, job.PayloadJson, job.DeviceId, pose);
    }

    private ValidationResult ValidateAndSimulate(string commandName, string payloadJson, int deviceId, GridPose pose)
    {
        if (commandName.Equals("PLACE", StringComparison.OrdinalIgnoreCase))
        {
            return TryReadPlacePose(payloadJson, deviceId, out var placedPose, out var failure)
                ? ValidationResult.Valid(placedPose)
                : ValidationResult.Invalid("PLACE_PAYLOAD_INVALID", failure ?? "PLACE requires gridX/gridY/facing payload.");
        }

        if (!DomainConstants.RequiresTrustedGridPose(commandName))
        {
            return ValidationResult.Valid(pose);
        }

        if (!pose.IsTrusted || !pose.IsAligned || !pose.MapId.HasValue || !pose.X.HasValue || !pose.Y.HasValue || string.IsNullOrWhiteSpace(pose.Facing))
        {
            return ValidationResult.Invalid("GRID_POSE_UNTRUSTED", "Grid movement requires trusted and aligned grid pose. Use PLACE first.");
        }

        var map = _mapDataAccess.GetMapById(pose.MapId.Value);
        if (map == null || !map.IsActive)
        {
            return ValidationResult.Invalid("MAP_NOT_AVAILABLE", "Grid movement requires an active map.");
        }

        var next = pose.Clone();

        if (commandName.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
        {
            next.Facing = TurnLeft(pose.Facing!);
            return ValidationResult.Valid(next);
        }

        if (commandName.Equals("RIGHT", StringComparison.OrdinalIgnoreCase))
        {
            next.Facing = TurnRight(pose.Facing!);
            return ValidationResult.Valid(next);
        }

        if (commandName.Equals("MOVE", StringComparison.OrdinalIgnoreCase))
        {
            MoveOneCell(next, +1);
        }
        else if (commandName.Equals("STEP_BACK", StringComparison.OrdinalIgnoreCase))
        {
            MoveOneCell(next, -1);
        }
        else
        {
            return ValidationResult.Valid(pose);
        }

        if (!IsOnMap(map, next.X!.Value, next.Y!.Value))
        {
            return ValidationResult.Invalid("GRID_OUT_OF_BOUNDS", $"{commandName} would move the robot outside the map.");
        }

        return ValidationResult.Valid(next);
    }

    private void ExpireStaleWork(int deviceId, DateTime now)
    {
        var staleJobs = _jobDataAccess.GetJobsByDeviceId(deviceId)
            .Where(x => (x.Status.Equals("Claimed", StringComparison.OrdinalIgnoreCase) || x.Status.Equals("Executing", StringComparison.OrdinalIgnoreCase))
                        && x.LeaseExpiresAtUtc.HasValue
                        && x.LeaseExpiresAtUtc.Value <= now)
            .OrderBy(x => x.CreatedDate)
            .ThenBy(x => x.Id)
            .ToList();

        foreach (var staleJob in staleJobs)
        {
            if (staleJob.Status.Equals("Claimed", StringComparison.OrdinalIgnoreCase))
            {
                staleJob.Status = "Queued";
                staleJob.ClaimedByDeviceCredentialId = null;
                staleJob.ClaimedAtUtc = null;
                staleJob.LeaseExpiresAtUtc = null;
                staleJob.ModifiedDate = now;
                _jobDataAccess.UpdateJob(staleJob.Id, staleJob);
                continue;
            }

            staleJob.Status = "Expired";
            staleJob.ModifiedDate = now;
            _jobDataAccess.UpdateJob(staleJob.Id, staleJob);
            InsertJobHistory(staleJob, false, true, null, "LEASE_EXPIRED", "Job lease expired while executing. It was not automatically retried.", now);
            InvalidatePose(staleJob.DeviceId, "Grid pose invalidated because an executing job expired.", now);

            if (staleJob.WorkflowId.HasValue)
            {
                var workflow = _workflowDataAccess.GetWorkflowById(staleJob.WorkflowId.Value);
                if (workflow != null)
                {
                    CancelQueuedWorkflowJobs(workflow.Id, now);
                    workflow.Status = "Failed";
                    workflow.ModifiedDate = now;
                    _workflowDataAccess.UpdateWorkflow(workflow.Id, workflow);
                    InsertWorkflowHistoryIfMissing(workflow, false, staleJob.StepNumber, "A workflow step expired while executing.", now);
                }
            }
        }
    }

    private void MarkParentWorkflowClaimed(int? workflowId, int deviceCredentialId, DateTime now, int leaseMinutes)
    {
        if (!workflowId.HasValue)
        {
            return;
        }

        var workflow = _workflowDataAccess.GetWorkflowById(workflowId.Value);
        if (workflow == null)
        {
            return;
        }

        if (IsTerminalStatus(workflow.Status))
        {
            return;
        }

        workflow.Status = workflow.Status == "Executing" ? "Executing" : "Claimed";
        workflow.ClaimedByDeviceCredentialId ??= deviceCredentialId;
        workflow.ClaimedAtUtc ??= now;
        workflow.LeaseExpiresAtUtc = now.AddMinutes(leaseMinutes);
        workflow.ModifiedDate = now;
        _workflowDataAccess.UpdateWorkflow(workflow.Id, workflow);
    }

    private void MarkJobValidationFailed(Job job, string failureCode, string failureMessage, bool executed, DateTime now)
    {
        job.Status = "Failed";
        job.ModifiedDate = now;
        _jobDataAccess.UpdateJob(job.Id, job);
        InsertJobHistory(job, false, executed, null, failureCode, failureMessage, now);
    }

    private void FailWorkflowBeforeDispatch(Workflow workflow, Job? failedJob, string failureCode, string failureMessage, DateTime now)
    {
        if (failedJob != null)
        {
            MarkJobValidationFailed(failedJob, failureCode, failureMessage, false, now);
        }

        CancelQueuedWorkflowJobs(workflow.Id, now);
        workflow.Status = "Failed";
        workflow.ModifiedDate = now;
        _workflowDataAccess.UpdateWorkflow(workflow.Id, workflow);
        InsertWorkflowHistoryIfMissing(workflow, false, failedJob?.StepNumber, failureMessage, now);
    }

    private void CancelQueuedWorkflowJobs(int workflowId, DateTime now)
    {
        foreach (var job in _jobDataAccess.GetJobsByWorkflowId(workflowId).Where(x => x.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase)))
        {
            job.Status = "Cancelled";
            job.ModifiedDate = now;
            _jobDataAccess.UpdateJob(job.Id, job);
        }
    }

    private void FinalizeParentWorkflowIfReady(int workflowId, DateTime now)
    {
        var workflow = _workflowDataAccess.GetWorkflowById(workflowId);
        if (workflow == null || IsTerminalStatus(workflow.Status)) return;

        var jobs = _jobDataAccess.GetJobsByWorkflowId(workflowId);
        if (jobs.Any(x => !IsTerminalStatus(x.Status))) return;

        var hasSuccessfulStep = jobs.Any(x => x.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));
        var success = workflow.ExecutionMode.Equals("BestEffort", StringComparison.OrdinalIgnoreCase)
            ? hasSuccessfulStep
            : jobs.All(x => x.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase));

        workflow.Status = success ? "Completed" : "Failed";
        workflow.ModifiedDate = now;
        _workflowDataAccess.UpdateWorkflow(workflow.Id, workflow);
        InsertWorkflowHistoryIfMissing(workflow, success, jobs.FirstOrDefault(x => !x.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase))?.StepNumber, success ? null : "Workflow finished with failed steps.", now);
    }

    private void InsertJobHistory(Job job, bool success, bool executed, string? resultJson, string? failureCode, string? failureMessage, DateTime now)
    {
        var command = _jobDataAccess.GetCommandCatalogueById(job.CommandCatalogueId);
        var commandName = command?.Name?.Trim() ?? $"CommandCatalogue:{job.CommandCatalogueId}";

        _jobHistoryDataAccess.InsertJobHistory(new JobHistory
        {
            JobId = job.Id,
            WorkflowId = job.WorkflowId,
            StepNumber = job.StepNumber,
            DeviceId = job.DeviceId,
            CommandCatalogueId = job.CommandCatalogueId,
            CommandName = commandName,
            PayloadJson = string.IsNullOrWhiteSpace(job.PayloadJson) ? "{}" : job.PayloadJson,
            ProviderType = job.ProviderType,
            ExecutionKind = NormalizeExecutionKind(command?.ExecutionKind),
            RollbackKind = NormalizeRollbackKind(command?.RollbackKind),
            Executed = executed,
            Success = success,
            ResultJson = resultJson,
            FailureCode = failureCode,
            FailureMessage = failureMessage,
            StartedAtUtc = executed ? job.ClaimedAtUtc : null,
            CompletedAtUtc = now,
            DurationMs = executed && job.ClaimedAtUtc.HasValue ? Math.Max(0, (int)Math.Round((now - job.ClaimedAtUtc.Value).TotalMilliseconds)) : null,
            RollbackOfJobHistoryId = job.RollbackOfJobHistoryId,
            CreatedDate = now
        });
    }

    private void InsertWorkflowHistoryIfMissing(Workflow workflow, bool success, int? failedStepNumber, string? failureMessage, DateTime now)
    {
        if (_workflowHistoryDataAccess.GetWorkflowHistoriesByWorkflowId(workflow.Id).Any()) return;

        _workflowHistoryDataAccess.InsertWorkflowHistory(new WorkflowHistory
        {
            WorkflowId = workflow.Id,
            DeviceId = workflow.DeviceId,
            ProviderType = workflow.ProviderType,
            Status = success ? "Completed" : "Failed",
            Executed = _jobDataAccess.GetJobsByWorkflowId(workflow.Id).Any(x => x.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)),
            Success = success,
            StartedAtUtc = workflow.ClaimedAtUtc,
            CompletedAtUtc = now,
            FailedStepNumber = failedStepNumber,
            FailureMessage = failureMessage,
            RollbackOfWorkflowHistoryId = workflow.RollbackOfWorkflowHistoryId,
            CreatedDate = now
        });
    }

    private GridPose BuildCurrentPose(int deviceId)
    {
        var status = _deviceStatusDataAccess.GetDeviceStatusByDeviceId(deviceId);
        return new GridPose
        {
            MapId = status?.PoseMapId ?? _jobDataAccess.GetDeviceMapId(deviceId),
            X = status?.GridX,
            Y = status?.GridY,
            Facing = status?.Facing,
            IsAligned = status?.IsGridAligned ?? false,
            IsTrusted = status?.IsGridPoseTrusted ?? false
        };
    }

    private bool TryReadPlacePose(string payloadJson, int deviceId, out GridPose pose, out string? failureMessage)
    {
        pose = new GridPose();
        failureMessage = null;

        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payloadJson) ? "{}" : payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                failureMessage = "PLACE payload must be a JSON object.";
                return false;
            }

            var root = document.RootElement;
            var x = TryGetInt(root, "gridX", "GridX", "x", "X");
            var y = TryGetInt(root, "gridY", "GridY", "y", "Y");
            var facing = TryGetString(root, "facing", "Facing", "direction", "Direction");
            var mapId = TryGetInt(root, "poseMapId", "PoseMapId", "mapId", "MapId") ?? _jobDataAccess.GetDeviceMapId(deviceId);

            if (!x.HasValue || !y.HasValue || string.IsNullOrWhiteSpace(facing))
            {
                failureMessage = "PLACE requires gridX, gridY, and facing/direction.";
                return false;
            }

            if (!mapId.HasValue)
            {
                failureMessage = "PLACE requires the device to have an assigned map or a poseMapId payload value.";
                return false;
            }

            facing = NormalizeFacing(facing!);
            if (facing == null)
            {
                failureMessage = "Facing must be North, East, South, or West.";
                return false;
            }

            var map = _mapDataAccess.GetMapById(mapId.Value);
            if (map == null || !map.IsActive)
            {
                failureMessage = "PLACE requires an active map.";
                return false;
            }

            if (!IsOnMap(map, x.Value, y.Value))
            {
                failureMessage = "PLACE coordinates are outside the map.";
                return false;
            }

            pose = new GridPose
            {
                MapId = mapId,
                X = x,
                Y = y,
                Facing = facing,
                IsAligned = true,
                IsTrusted = true
            };
            return true;
        }
        catch (JsonException)
        {
            failureMessage = "PLACE payload must be valid JSON.";
            return false;
        }
    }

    private void InvalidatePose(int deviceId, string reason, DateTime now)
    {
        var status = _deviceStatusDataAccess.GetDeviceStatusByDeviceId(deviceId);
        if (status == null) return;
        status.IsGridAligned = false;
        status.IsGridPoseTrusted = false;
        status.PoseConfidence = 0;
        status.StatusMessage = reason;
        status.ModifiedDate = now;
        _deviceStatusDataAccess.UpdateDeviceStatus(status.Id, status);
    }

    private static int? TryGetInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)) return number;
            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)) return parsed;
        }

        return null;
    }

    private static string? TryGetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value)) continue;
            if (value.ValueKind == JsonValueKind.String) return value.GetString();
        }

        return null;
    }

    private static string? NormalizeFacing(string value)
    {
        if (value.Equals("North", StringComparison.OrdinalIgnoreCase)) return "North";
        if (value.Equals("East", StringComparison.OrdinalIgnoreCase)) return "East";
        if (value.Equals("South", StringComparison.OrdinalIgnoreCase)) return "South";
        if (value.Equals("West", StringComparison.OrdinalIgnoreCase)) return "West";
        return null;
    }

    private static string TurnLeft(string facing) => facing switch
    {
        "North" => "West",
        "West" => "South",
        "South" => "East",
        "East" => "North",
        _ => facing
    };

    private static string TurnRight(string facing) => facing switch
    {
        "North" => "East",
        "East" => "South",
        "South" => "West",
        "West" => "North",
        _ => facing
    };

    private static void MoveOneCell(GridPose pose, int sign)
    {
        if (pose.Facing == "North") pose.Y += sign;
        else if (pose.Facing == "East") pose.X += sign;
        else if (pose.Facing == "South") pose.Y -= sign;
        else if (pose.Facing == "West") pose.X -= sign;
    }

    private static bool IsOnMap(Map map, int x, int y)
    {
        return x >= 0 && x < map.Columns && y >= 0 && y < map.Rows;
    }

    private static bool IsTerminalStatus(string? status)
    {
        return status != null &&
               (status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Expired", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("RolledBack", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeExecutionKind(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        return DomainConstants.IsCommandExecutionKind(candidate) ? candidate : "Mode";
    }

    private static string NormalizeRollbackKind(string? value)
    {
        var candidate = value?.Trim() ?? string.Empty;
        return DomainConstants.IsCommandRollbackKind(candidate) ? candidate : "None";
    }

    private enum DispatchDecision
    {
        Claim,
        SkipAndContinue,
        StopWithoutWork
    }

    private sealed class GridPose
    {
        public int? MapId { get; set; }
        public int? X { get; set; }
        public int? Y { get; set; }
        public string? Facing { get; set; }
        public bool IsAligned { get; set; }
        public bool IsTrusted { get; set; }

        public GridPose Clone() => new()
        {
            MapId = MapId,
            X = X,
            Y = Y,
            Facing = Facing,
            IsAligned = IsAligned,
            IsTrusted = IsTrusted
        };
    }

    private sealed class ValidationResult
    {
        public bool IsValid { get; init; }
        public string FailureCode { get; init; } = string.Empty;
        public string FailureMessage { get; init; } = string.Empty;
        public GridPose? NextPose { get; init; }
        public Job? Job { get; set; }

        public static ValidationResult Valid(GridPose? nextPose = null) => new() { IsValid = true, NextPose = nextPose };
        public static ValidationResult Invalid(string failureCode, string failureMessage) => new() { IsValid = false, FailureCode = failureCode, FailureMessage = failureMessage };
    }
}
