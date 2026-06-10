using RobotControllerApi.BoundedContexts.Jobs.Dtos;
using RobotControllerApi.BoundedContexts.Jobs.Models;
using RobotControllerApi.BoundedContexts.Jobs.Persistence;
using RobotControllerApi.BoundedContexts.Jobs.Services;
using RobotControllerApi.BoundedContexts.Shared;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Models;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Persistence;
using RobotControllerApi.BoundedContexts.Workflows.Dtos;
using RobotControllerApi.BoundedContexts.Workflows.Models;
using RobotControllerApi.BoundedContexts.Workflows.Persistence;

namespace RobotControllerApi.BoundedContexts.Workflows.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowDataAccess _dataAccess;
    private readonly IJobDataAccess _jobDataAccess;
    private readonly IJobService _jobService;
    private readonly IWorkflowHistoryDataAccess _workflowHistoryDataAccess;
    private readonly IConfiguration _configuration;

    public WorkflowService(
        IWorkflowDataAccess dataAccess,
        IJobDataAccess jobDataAccess,
        IJobService jobService,
        IWorkflowHistoryDataAccess workflowHistoryDataAccess,
        IConfiguration configuration)
    {
        _dataAccess = dataAccess;
        _jobDataAccess = jobDataAccess;
        _jobService = jobService;
        _workflowHistoryDataAccess = workflowHistoryDataAccess;
        _configuration = configuration;
    }

    public List<WorkflowResponse> GetWorkflows() => _dataAccess.GetWorkflows().Select(MapToResponse).ToList();
    public WorkflowResponse? GetWorkflowById(int id) => _dataAccess.GetWorkflowById(id) is { } model ? MapToResponse(model) : null;
    public List<WorkflowResponse> GetWorkflowsByDeviceId(int deviceId) => _dataAccess.GetWorkflowsByDeviceId(deviceId).Select(MapToResponse).ToList();

    public WorkflowWithJobsResponse? GetWorkflowWithJobsById(int id)
    {
        var model = _dataAccess.GetWorkflowById(id);
        return model == null ? null : MapToWithJobsResponse(model);
    }

    public WorkflowWithJobsResponse CreateWorkflow(int deviceId, CreateWorkflowRequest request, int? requestedByAppUserId = null)
    {
        ValidateWorkflow(deviceId, request.Name, request.Description, request.SchemaVersion, request.IsRollback ? "AllOrNothing" : request.ExecutionMode, request.IsRollback ? "Rollback" : request.ProviderType, "Queued", request.IsRollback);
        if (request.Steps.Count == 0) throw new ArgumentException("At least one workflow step is required.");
        if (request.Steps.Select(x => x.StepNumber).Distinct().Count() != request.Steps.Count) throw new ArgumentException("Workflow step numbers must be unique.");

        foreach (var step in request.Steps.OrderBy(x => x.StepNumber))
        {
            ValidateChildJob(deviceId, step.CommandCatalogueId, step.PayloadJson);
        }

        EnsureDeviceQueueHasCapacity(deviceId);

        var now = DateTime.UtcNow;
        var workflow = _dataAccess.InsertWorkflow(new Workflow
        {
            DeviceId = deviceId,
            Name = request.Name.Trim(),
            Description = request.Description,
            SchemaVersion = request.SchemaVersion.Trim(),
            ExecutionMode = request.IsRollback ? "AllOrNothing" : request.ExecutionMode,
            ProviderType = request.IsRollback ? "Rollback" : request.ProviderType,
            Status = "Queued",
            RequestedByAppUserId = requestedByAppUserId ?? request.RequestedByAppUserId,
            IsRollback = request.IsRollback,
            RollbackOfWorkflowHistoryId = request.RollbackOfWorkflowHistoryId,
            CreatedDate = now,
            ModifiedDate = now
        });

        foreach (var step in request.Steps.OrderBy(x => x.StepNumber))
        {
            _jobDataAccess.InsertJob(new Job
            {
                DeviceId = deviceId,
                WorkflowId = workflow.Id,
                StepNumber = step.StepNumber,
                CommandCatalogueId = step.CommandCatalogueId,
                PayloadJson = string.IsNullOrWhiteSpace(step.PayloadJson) ? "{}" : step.PayloadJson,
                ProviderType = workflow.ProviderType,
                Status = "Queued",
                RequestedByAppUserId = requestedByAppUserId ?? request.RequestedByAppUserId,
                IsRollback = request.IsRollback,
                CreatedDate = now,
                ModifiedDate = now
            });
        }

        return MapToWithJobsResponse(workflow);
    }

    public bool UpdateWorkflow(int id, UpdateWorkflowRequest request)
    {
        var existing = _dataAccess.GetWorkflowById(id);
        if (existing == null) return false;
        ValidateWorkflow(request.DeviceId, request.Name, request.Description, request.SchemaVersion, request.IsRollback ? "AllOrNothing" : request.ExecutionMode, request.IsRollback ? "Rollback" : request.ProviderType, request.Status, request.IsRollback);
        existing.DeviceId = request.DeviceId;
        existing.Name = request.Name.Trim();
        existing.Description = request.Description;
        existing.SchemaVersion = request.SchemaVersion.Trim();
        existing.ExecutionMode = request.IsRollback ? "AllOrNothing" : request.ExecutionMode;
        existing.ProviderType = request.IsRollback ? "Rollback" : request.ProviderType;
        existing.Status = request.Status;
        existing.RequestedByAppUserId = request.RequestedByAppUserId;
        existing.ClaimedByDeviceCredentialId = request.ClaimedByDeviceCredentialId;
        existing.ClaimedAtUtc = request.ClaimedAtUtc;
        existing.LeaseExpiresAtUtc = request.LeaseExpiresAtUtc;
        existing.IsRollback = request.IsRollback;
        existing.RollbackOfWorkflowHistoryId = request.RollbackOfWorkflowHistoryId;
        existing.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateWorkflow(existing.Id, existing);
    }

    public bool DeleteWorkflow(int id)
    {
        if (_dataAccess.GetWorkflowById(id) == null) return false;
        return _dataAccess.DeleteWorkflow(id);
    }

    public bool CancelWorkflow(int id)
    {
        var existing = _dataAccess.GetWorkflowById(id);
        if (existing == null) return false;
        if (existing.Status is "Completed" or "Failed" or "RolledBack") throw new InvalidOperationException("Completed, failed, or rolled-back workflows cannot be cancelled.");
        existing.Status = "Cancelled";
        existing.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateWorkflow(existing.Id, existing);
    }

    public bool MarkWorkflowStarted(int id, int? deviceCredentialId = null)
    {
        var existing = _dataAccess.GetWorkflowById(id);
        if (existing == null) return false;
        ValidateClaimedCredential(existing, deviceCredentialId);
        if (deviceCredentialId.HasValue && existing.ClaimedByDeviceCredentialId == null) existing.ClaimedByDeviceCredentialId = deviceCredentialId;
        if (existing.Status is "Completed" or "Cancelled") throw new InvalidOperationException("This workflow cannot be started from its current status.");
        existing.Status = "Executing";
        existing.ModifiedDate = DateTime.UtcNow;
        return _dataAccess.UpdateWorkflow(existing.Id, existing);
    }

    public bool MarkWorkflowCompleted(int id, CompleteWorkflowRequest request, int? deviceCredentialId = null)
    {
        var existing = _dataAccess.GetWorkflowById(id);
        if (existing == null) return false;
        ValidateClaimedCredential(existing, deviceCredentialId);
        if (deviceCredentialId.HasValue && existing.ClaimedByDeviceCredentialId == null) existing.ClaimedByDeviceCredentialId = deviceCredentialId;
        existing.Status = "Completed";
        existing.ModifiedDate = DateTime.UtcNow;
        var updated = _dataAccess.UpdateWorkflow(existing.Id, existing);
        if (updated) InsertWorkflowHistoryIfMissing(existing, true, null, null, existing.ModifiedDate);
        return updated;
    }

    public bool MarkWorkflowFailed(int id, FailWorkflowRequest request, int? deviceCredentialId = null)
    {
        var existing = _dataAccess.GetWorkflowById(id);
        if (existing == null) return false;
        ValidateClaimedCredential(existing, deviceCredentialId);
        if (deviceCredentialId.HasValue && existing.ClaimedByDeviceCredentialId == null) existing.ClaimedByDeviceCredentialId = deviceCredentialId;
        existing.Status = "Failed";
        existing.ModifiedDate = DateTime.UtcNow;
        var updated = _dataAccess.UpdateWorkflow(existing.Id, existing);
        if (updated) InsertWorkflowHistoryIfMissing(existing, false, request.FailedStepNumber, string.IsNullOrWhiteSpace(request.FailureMessage) ? "Workflow failed." : request.FailureMessage, existing.ModifiedDate);
        return updated;
    }

    private static void ValidateClaimedCredential(Workflow existing, int? deviceCredentialId)
    {
        if (!deviceCredentialId.HasValue)
        {
            return;
        }

        if (existing.ClaimedByDeviceCredentialId.HasValue && existing.ClaimedByDeviceCredentialId.Value != deviceCredentialId.Value)
        {
            throw new UnauthorizedAccessException("This device credential did not claim this workflow.");
        }
    }

    private void EnsureDeviceQueueHasCapacity(int deviceId)
    {
        var maxQueuedItems = int.TryParse(_configuration["WorkDispatch:MaxQueuedWorkItemsPerDevice"], out var parsed) ? parsed : 3;

        var queuedWorkflows = _dataAccess.GetWorkflowsByDeviceId(deviceId)
            .Count(x => x.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase));

        var queuedStandaloneJobs = _jobDataAccess.GetJobsByDeviceId(deviceId)
            .Count(x => x.WorkflowId == null && x.Status.Equals("Queued", StringComparison.OrdinalIgnoreCase));

        if (queuedWorkflows + queuedStandaloneJobs >= maxQueuedItems)
        {
            throw new InvalidOperationException($"Device {deviceId} already has {maxQueuedItems} queued work item(s). Wait for queued work to be claimed or cancel stale work before queueing more.");
        }
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

    private void ValidateWorkflow(int deviceId, string name, string? description, string schemaVersion, string executionMode, string providerType, string status, bool isRollback)
    {
        if (!_dataAccess.DeviceExistsAndActive(deviceId)) throw new ArgumentException("Device must exist and be active.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (name.Length > 200) throw new ArgumentException("Name cannot exceed 200 characters.");
        if (description != null && description.Length > 1000) throw new ArgumentException("Description cannot exceed 1000 characters.");
        if (string.IsNullOrWhiteSpace(schemaVersion)) throw new ArgumentException("SchemaVersion is required.");
        if (!DomainConstants.IsExecutionMode(executionMode)) throw new ArgumentException("ExecutionMode must be BestEffort or AllOrNothing.");
        if (!DomainConstants.IsProviderType(providerType)) throw new ArgumentException("ProviderType must be Api, Console, File, Poll, or Rollback.");
        if (!DomainConstants.IsWorkStatus(status)) throw new ArgumentException("Status must be Queued, Claimed, Executing, Completed, Failed, Cancelled, Expired, or RolledBack.");
        if (isRollback && !providerType.Equals("Rollback", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("If IsRollback is true, ProviderType must be Rollback.");
        if (isRollback && !executionMode.Equals("AllOrNothing", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Rollback workflows must use ExecutionMode AllOrNothing.");
    }

    private void ValidateChildJob(int deviceId, int commandCatalogueId, string payloadJson)
    {
        JobService.ValidateJsonObject(payloadJson, "PayloadJson");
        if (!_jobDataAccess.CommandCatalogueExistsAndActive(commandCatalogueId)) throw new ArgumentException("All child job commands must be active command catalogue rows.");
        var capability = _jobDataAccess.GetActiveDeviceCapability(deviceId, commandCatalogueId);
        if (capability == null) throw new ArgumentException("All child job commands must be valid for the target device.");
        if (capability.RequiresMap && _jobDataAccess.GetDeviceMapId(deviceId) == null) throw new ArgumentException("A child job command requires the device to have an assigned map.");
    }

    public static WorkflowResponse MapToResponse(Workflow model) => new()
    {
        Id = model.Id,
        DeviceId = model.DeviceId,
        Name = model.Name,
        Description = model.Description,
        SchemaVersion = model.SchemaVersion,
        ExecutionMode = model.ExecutionMode,
        ProviderType = model.ProviderType,
        Status = model.Status,
        RequestedByAppUserId = model.RequestedByAppUserId,
        ClaimedByDeviceCredentialId = model.ClaimedByDeviceCredentialId,
        ClaimedAtUtc = model.ClaimedAtUtc,
        LeaseExpiresAtUtc = model.LeaseExpiresAtUtc,
        IsRollback = model.IsRollback,
        RollbackOfWorkflowHistoryId = model.RollbackOfWorkflowHistoryId,
        CreatedDate = model.CreatedDate,
        ModifiedDate = model.ModifiedDate
    };

    private WorkflowWithJobsResponse MapToWithJobsResponse(Workflow model) => new()
    {
        Id = model.Id,
        DeviceId = model.DeviceId,
        Name = model.Name,
        Description = model.Description,
        SchemaVersion = model.SchemaVersion,
        ExecutionMode = model.ExecutionMode,
        ProviderType = model.ProviderType,
        Status = model.Status,
        RequestedByAppUserId = model.RequestedByAppUserId,
        ClaimedByDeviceCredentialId = model.ClaimedByDeviceCredentialId,
        ClaimedAtUtc = model.ClaimedAtUtc,
        LeaseExpiresAtUtc = model.LeaseExpiresAtUtc,
        IsRollback = model.IsRollback,
        RollbackOfWorkflowHistoryId = model.RollbackOfWorkflowHistoryId,
        CreatedDate = model.CreatedDate,
        ModifiedDate = model.ModifiedDate,
        Jobs = _jobService.GetJobsByWorkflowId(model.Id)
    };
}
