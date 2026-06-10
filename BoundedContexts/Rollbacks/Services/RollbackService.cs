using System.Text.Json;
using System.Text.Json.Nodes;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.JobHistories.Models;
using RobotControllerApi.BoundedContexts.JobHistories.Persistence;
using RobotControllerApi.BoundedContexts.Jobs.Models;
using RobotControllerApi.BoundedContexts.Jobs.Persistence;
using RobotControllerApi.BoundedContexts.Rollbacks.Dtos;
using RobotControllerApi.BoundedContexts.Rollbacks.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Persistence;
using RobotControllerApi.BoundedContexts.Shared;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Persistence;
using RobotControllerApi.BoundedContexts.Workflows.Models;
using RobotControllerApi.BoundedContexts.Workflows.Persistence;

namespace RobotControllerApi.BoundedContexts.Rollbacks.Services;

public class RollbackService : IRollbackService
{
    private readonly IRollbackRequestDataAccess _rollbackDataAccess;
    private readonly IJobHistoryDataAccess _jobHistoryDataAccess;
    private readonly IWorkflowHistoryDataAccess _workflowHistoryDataAccess;
    private readonly IJobDataAccess _jobDataAccess;
    private readonly IWorkflowDataAccess _workflowDataAccess;

    public RollbackService(IRollbackRequestDataAccess rollbackDataAccess, IJobHistoryDataAccess jobHistoryDataAccess, IWorkflowHistoryDataAccess workflowHistoryDataAccess, IJobDataAccess jobDataAccess, IWorkflowDataAccess workflowDataAccess)
    {
        _rollbackDataAccess = rollbackDataAccess;
        _jobHistoryDataAccess = jobHistoryDataAccess;
        _workflowHistoryDataAccess = workflowHistoryDataAccess;
        _jobDataAccess = jobDataAccess;
        _workflowDataAccess = workflowDataAccess;
    }

    public List<RollbackRequestResponse> GetRollbackRequests() => _rollbackDataAccess.GetRollbackRequests().Select(MapToResponse).ToList();
    public RollbackRequestResponse? GetRollbackRequestById(int id) => _rollbackDataAccess.GetRollbackRequestById(id) is { } model ? MapToResponse(model) : null;
    public List<RollbackRequestResponse> GetRollbackRequestsByDeviceId(int deviceId) => _rollbackDataAccess.GetRollbackRequestsByDeviceId(deviceId).Select(MapToResponse).ToList();

    public RollbackGenerationResponse GenerateRollbackForJob(int jobId, CreateRollbackFromJobHistoryRequest request)
    {
        var histories = _jobHistoryDataAccess.GetJobHistoriesByJobId(jobId);
        if (histories.Count == 0) throw new ArgumentException("No job history rows exist for the requested job.");
        request.JobHistoryIds = histories.Select(x => x.Id).ToList();
        return GenerateRollbackFromJobHistories(request);
    }

    public RollbackGenerationResponse GenerateRollbackForWorkflow(int workflowId, CreateRollbackFromWorkflowHistoryRequest request)
    {
        var histories = _jobHistoryDataAccess.GetJobHistoriesByWorkflowId(workflowId);
        if (histories.Count == 0) throw new ArgumentException("No job history rows exist for the requested workflow.");
        return GenerateRollbackFromHistories(histories, request.RequestedByAppUserId, request.RequestedByProvider, "WorkflowHistory", $"[{workflowId}]", null, request.DeviceId, request.AllowDuplicate);
    }

    public RollbackGenerationResponse GenerateRollbackFromJobHistories(CreateRollbackFromJobHistoryRequest request)
    {
        if (request.JobHistoryIds.Count == 0) throw new ArgumentException("At least one JobHistory id is required.");
        var histories = request.JobHistoryIds.Select(id => _jobHistoryDataAccess.GetJobHistoryById(id) ?? throw new ArgumentException($"JobHistory id {id} does not exist.")).ToList();
        return GenerateRollbackFromHistories(histories, request.RequestedByAppUserId, request.RequestedByProvider, "JobHistory", SerializeIds(request.JobHistoryIds), null, request.DeviceId, request.AllowDuplicate);
    }

    public RollbackGenerationResponse GenerateRollbackFromWorkflowHistories(CreateRollbackFromWorkflowHistoryRequest request)
    {
        if (request.WorkflowHistoryIds.Count == 0) throw new ArgumentException("At least one WorkflowHistory id is required.");
        var workflowHistories = request.WorkflowHistoryIds.Select(id => _workflowHistoryDataAccess.GetWorkflowHistoryById(id) ?? throw new ArgumentException($"WorkflowHistory id {id} does not exist.")).ToList();
        var workflowIds = workflowHistories.Where(x => x.WorkflowId != null).Select(x => x.WorkflowId!.Value).Distinct().ToList();
        if (workflowIds.Count == 0) throw new ArgumentException("WorkflowHistory rows must reference workflow rows to generate rollback work.");
        var histories = workflowIds.SelectMany(id => _jobHistoryDataAccess.GetJobHistoriesByWorkflowId(id)).ToList();
        if (histories.Count == 0) throw new ArgumentException("No job history rows exist for the requested workflow history rows.");
        return GenerateRollbackFromHistories(histories, request.RequestedByAppUserId, request.RequestedByProvider, "WorkflowHistory", SerializeIds(request.WorkflowHistoryIds), workflowHistories.First().Id, request.DeviceId, request.AllowDuplicate);
    }

    private RollbackGenerationResponse GenerateRollbackFromHistories(List<JobHistory> histories, int? requestedByAppUserId, string requestedByProvider, string targetType, string targetIdsJson, int? rollbackOfWorkflowHistoryId, int requestedDeviceId = 0, bool allowDuplicate = false)
    {
        if (!DomainConstants.IsProviderType(requestedByProvider)) throw new ArgumentException("RequestedByProvider must be Api, Console, File, Poll, or Rollback.");
        if (!DomainConstants.IsRollbackTargetType(targetType)) throw new ArgumentException("TargetType must be JobHistory or WorkflowHistory.");
        if (histories.Count == 0) throw new ArgumentException("History target rows are required.");

        var deviceIds = histories.Select(x => x.DeviceId).Distinct().ToList();
        if (deviceIds.Count != 1) throw new ArgumentException("All target history rows must belong to the same device.");
        var deviceId = deviceIds[0];
        if (requestedDeviceId > 0 && requestedDeviceId != deviceId) throw new ArgumentException("Requested DeviceId must match all target history rows.");
        if (!_rollbackDataAccess.DeviceExistsAndActive(deviceId)) throw new ArgumentException("Device must exist and be active.");
        if (!allowDuplicate && _rollbackDataAccess.HasGeneratedRollbackForTarget(targetType, targetIdsJson)) throw new InvalidOperationException("Duplicate rollback work for the same target has already been generated.");

        // Do not generate rollback jobs/workflows while the device already has active work.
        // Otherwise a repeated rollback POST can create a large backlog that the Nano will
        // keep claiming and executing.
        EnsureDeviceHasNoActiveWork(deviceId);

        var now = DateTime.UtcNow;
        var rollbackRequest = _rollbackDataAccess.InsertRollbackRequest(new RollbackRequest
        {
            DeviceId = deviceId,
            RequestedByAppUserId = requestedByAppUserId,
            RequestedByProvider = requestedByProvider,
            TargetType = targetType,
            TargetIdsJson = targetIdsJson,
            Status = "Requested",
            CreatedDate = now,
            ModifiedDate = now
        });

        try
        {
            var steps = BuildRollbackSteps(histories);
            if (steps.Count == 0) throw new InvalidOperationException("No reversible successful executed commands exist for the requested target.");

            if (steps.Count == 1)
            {
                var step = steps[0];
                var job = _jobDataAccess.InsertJob(new Job
                {
                    DeviceId = deviceId,
                    CommandCatalogueId = step.CommandCatalogueId,
                    PayloadJson = step.PayloadJson,
                    ProviderType = "Rollback",
                    Status = "Queued",
                    RequestedByAppUserId = requestedByAppUserId,
                    IsRollback = true,
                    RollbackOfJobHistoryId = step.RollbackOfJobHistoryId,
                    CreatedDate = now,
                    ModifiedDate = now
                });
                rollbackRequest.Status = "Generated";
                rollbackRequest.GeneratedJobId = job.Id;
                rollbackRequest.ModifiedDate = DateTime.UtcNow;
                _rollbackDataAccess.UpdateRollbackRequest(rollbackRequest.Id, rollbackRequest);
                return new RollbackGenerationResponse { RollbackRequest = MapToResponse(rollbackRequest), GeneratedJobId = job.Id, Status = rollbackRequest.Status, Message = "Rollback job generated." };
            }

            var workflow = _workflowDataAccess.InsertWorkflow(new Workflow
            {
                DeviceId = deviceId,
                Name = $"Rollback {targetType} {rollbackRequest.Id}",
                Description = "Generated rollback workflow.",
                SchemaVersion = "1.0",
                ExecutionMode = "AllOrNothing",
                ProviderType = "Rollback",
                Status = "Queued",
                RequestedByAppUserId = requestedByAppUserId,
                IsRollback = true,
                RollbackOfWorkflowHistoryId = rollbackOfWorkflowHistoryId,
                CreatedDate = now,
                ModifiedDate = now
            });

            var stepNumber = 1;
            foreach (var step in steps)
            {
                _jobDataAccess.InsertJob(new Job
                {
                    DeviceId = deviceId,
                    WorkflowId = workflow.Id,
                    StepNumber = stepNumber++,
                    CommandCatalogueId = step.CommandCatalogueId,
                    PayloadJson = step.PayloadJson,
                    ProviderType = "Rollback",
                    Status = "Queued",
                    RequestedByAppUserId = requestedByAppUserId,
                    IsRollback = true,
                    RollbackOfJobHistoryId = step.RollbackOfJobHistoryId,
                    CreatedDate = now,
                    ModifiedDate = now
                });
            }

            rollbackRequest.Status = "Generated";
            rollbackRequest.GeneratedWorkflowId = workflow.Id;
            rollbackRequest.ModifiedDate = DateTime.UtcNow;
            _rollbackDataAccess.UpdateRollbackRequest(rollbackRequest.Id, rollbackRequest);
            return new RollbackGenerationResponse { RollbackRequest = MapToResponse(rollbackRequest), GeneratedWorkflowId = workflow.Id, Status = rollbackRequest.Status, Message = "Rollback workflow generated." };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            rollbackRequest.Status = "Failed";
            rollbackRequest.FailureCode = ex.GetType().Name;
            rollbackRequest.FailureMessage = ex.Message;
            rollbackRequest.ModifiedDate = DateTime.UtcNow;
            _rollbackDataAccess.UpdateRollbackRequest(rollbackRequest.Id, rollbackRequest);
            return new RollbackGenerationResponse { RollbackRequest = MapToResponse(rollbackRequest), Status = rollbackRequest.Status, Message = ex.Message };
        }
    }

    private void EnsureDeviceHasNoActiveWork(int deviceId)
    {
        var activeWorkflow = _workflowDataAccess.GetWorkflowsByDeviceId(deviceId)
            .Where(x => IsActiveWorkStatus(x.Status))
            .OrderBy(x => x.CreatedDate)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        if (activeWorkflow != null)
        {
            throw new InvalidOperationException($"Device {deviceId} already has active workflow {activeWorkflow.Id} ({activeWorkflow.Status}). Complete, fail, or cancel active work before generating rollback work.");
        }

        var activeJob = _jobDataAccess.GetJobsByDeviceId(deviceId)
            .Where(x => IsActiveWorkStatus(x.Status))
            .OrderBy(x => x.CreatedDate)
            .ThenBy(x => x.Id)
            .FirstOrDefault();

        if (activeJob != null)
        {
            throw new InvalidOperationException($"Device {deviceId} already has active job {activeJob.Id} ({activeJob.Status}). Complete, fail, or cancel active work before generating rollback work.");
        }
    }

    private static bool IsActiveWorkStatus(string? status)
    {
        return status != null &&
               (status.Equals("Queued", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Claimed", StringComparison.OrdinalIgnoreCase) ||
                status.Equals("Executing", StringComparison.OrdinalIgnoreCase));
    }

    private List<RollbackStep> BuildRollbackSteps(List<JobHistory> histories)
    {
        var successful = histories
            .Where(x => x.Executed && x.Success)
            .OrderByDescending(x => x.CreatedDate)
            .ThenByDescending(x => x.Id)
            .ToList();

        var result = new List<RollbackStep>();

        foreach (var history in successful)
        {
            var originalCommand = GetOriginalCommand(history);
            var originalCommandName = originalCommand.Name.Trim().ToUpperInvariant();

            if (originalCommandName == "PLACE") break;

            var rollbackKind = ResolveRollbackKind(history, originalCommand);
            if (rollbackKind.Equals("None", StringComparison.OrdinalIgnoreCase)) continue;

            if (!rollbackKind.Equals("Exact", StringComparison.OrdinalIgnoreCase) && !rollbackKind.Equals("BestEffort", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported RollbackKind {rollbackKind} for command {originalCommand.Name}.");
            }

            if (string.IsNullOrWhiteSpace(originalCommand.InverseCommandName))
            {
                throw new InvalidOperationException($"No inverse command is configured for command {originalCommand.Name}.");
            }

            var inverseCommandName = originalCommand.InverseCommandName.Trim();
            var inverseCommand = _rollbackDataAccess.GetActiveCommandCatalogueByName(inverseCommandName)
                ?? throw new InvalidOperationException($"No active CommandCatalogue row exists for rollback command {inverseCommandName}.");

            var capability = _jobDataAccess.GetActiveDeviceCapability(history.DeviceId, inverseCommand.Id)
                ?? throw new InvalidOperationException($"Device {history.DeviceId} cannot execute rollback command {inverseCommand.Name}.");

            if (capability.RequiresMap && _jobDataAccess.GetDeviceMapId(history.DeviceId) == null)
            {
                throw new InvalidOperationException($"Rollback command {inverseCommand.Name} requires the device to have an assigned map.");
            }

            if (DomainConstants.RequiresTrustedGridPose(inverseCommand.Name) && !_jobDataAccess.IsDeviceGridPoseTrustedAndAligned(history.DeviceId))
            {
                throw new InvalidOperationException($"Rollback command {inverseCommand.Name} requires trusted and aligned device pose.");
            }

            result.Add(new RollbackStep(inverseCommand.Id, TransformPayload(history, originalCommand, inverseCommand, rollbackKind), history.Id));
        }

        return result;
    }

    private CommandCatalogue GetOriginalCommand(JobHistory history)
    {
        if (history.CommandCatalogueId.HasValue)
        {
            var byId = _rollbackDataAccess.GetActiveCommandCatalogueById(history.CommandCatalogueId.Value);
            if (byId != null) return byId;
        }

        if (!string.IsNullOrWhiteSpace(history.CommandName))
        {
            var byName = _rollbackDataAccess.GetActiveCommandCatalogueByName(history.CommandName.Trim());
            if (byName != null) return byName;
        }

        throw new InvalidOperationException($"No active CommandCatalogue row exists for history id {history.Id}.");
    }

    private static string ResolveRollbackKind(JobHistory history, CommandCatalogue originalCommand)
    {
        if (!string.IsNullOrWhiteSpace(history.RollbackKind))
        {
            return history.RollbackKind.Trim();
        }

        return originalCommand.RollbackKind.Trim();
    }

    private static string TransformPayload(JobHistory history, CommandCatalogue originalCommand, CommandCatalogue inverseCommand, string rollbackKind)
    {
        var node = JsonNode.Parse(string.IsNullOrWhiteSpace(history.PayloadJson) ? "{}" : history.PayloadJson) as JsonObject ?? new JsonObject();

        var originalName = originalCommand.Name.Trim().ToUpperInvariant();
        var inverseName = inverseCommand.Name.Trim().ToUpperInvariant();

        if (originalName == inverseName)
        {
            if (originalName == "DRIVE_DISTANCE") NegateNumber(node, "distance", "distanceCm", "centimeters", "value");
            if (originalName == "ROTATE_DEGREES") NegateNumber(node, "degrees", "angleDegrees", "value");
        }

        var durationMs = history.DurationMs ?? TryGetInt(node, "durationMs", "duration", "milliseconds", "ms");
        var shouldPreserveDuration =
            inverseCommand.RequiresDuration ||
            originalCommand.ExecutionKind.Equals("Continuous", StringComparison.OrdinalIgnoreCase) ||
            inverseCommand.ExecutionKind.Equals("Continuous", StringComparison.OrdinalIgnoreCase) ||
            rollbackKind.Equals("BestEffort", StringComparison.OrdinalIgnoreCase);

        if (shouldPreserveDuration)
        {
            if (!durationMs.HasValue || durationMs.Value <= 0)
            {
                throw new InvalidOperationException($"Rollback command {inverseCommand.Name} requires a valid durationMs value.");
            }

            node["durationMs"] = durationMs.Value;
        }

        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static int? TryGetInt(JsonObject node, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!node.TryGetPropertyValue(name, out var value) || value == null) continue;

            try
            {
                return value.GetValue<int>();
            }
            catch (InvalidOperationException)
            {
                try
                {
                    return Convert.ToInt32(value.GetValue<double>());
                }
                catch
                {
                    // Ignore and continue to the next possible property name.
                }
            }
            catch (FormatException)
            {
                continue;
            }
        }

        return null;
    }

    private static void NegateNumber(JsonObject node, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!node.TryGetPropertyValue(name, out var value) || value == null) continue;
            try
            {
                var number = value.GetValue<double>();
                node[name] = -number;
                return;
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            catch (FormatException)
            {
                continue;
            }
        }
    }

    private static string SerializeIds(List<int> ids) => JsonSerializer.Serialize(ids.OrderBy(x => x).ToList());

    private static RollbackRequestResponse MapToResponse(RollbackRequest model) => new()
    {
        Id = model.Id,
        DeviceId = model.DeviceId,
        RequestedByAppUserId = model.RequestedByAppUserId,
        RequestedByProvider = model.RequestedByProvider,
        TargetType = model.TargetType,
        TargetIdsJson = model.TargetIdsJson,
        Status = model.Status,
        GeneratedWorkflowId = model.GeneratedWorkflowId,
        GeneratedJobId = model.GeneratedJobId,
        FailureCode = model.FailureCode,
        FailureMessage = model.FailureMessage,
        CreatedDate = model.CreatedDate,
        ModifiedDate = model.ModifiedDate
    };

    private sealed record RollbackStep(int CommandCatalogueId, string PayloadJson, int RollbackOfJobHistoryId);
}
