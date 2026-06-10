using RobotControllerApi.BoundedContexts.Shared;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Dtos;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Models;
using RobotControllerApi.BoundedContexts.WorkflowHistories.Persistence;

namespace RobotControllerApi.BoundedContexts.WorkflowHistories.Services;

public class WorkflowHistoryService : IWorkflowHistoryService
{
    private readonly IWorkflowHistoryDataAccess _dataAccess;

    public WorkflowHistoryService(IWorkflowHistoryDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public List<WorkflowHistoryResponse> GetWorkflowHistories() => _dataAccess.GetWorkflowHistories().Select(MapToResponse).ToList();
    public WorkflowHistoryResponse? GetWorkflowHistoryById(int id) => _dataAccess.GetWorkflowHistoryById(id) is { } model ? MapToResponse(model) : null;
    public List<WorkflowHistoryResponse> GetWorkflowHistoriesByWorkflowId(int workflowId) => _dataAccess.GetWorkflowHistoriesByWorkflowId(workflowId).Select(MapToResponse).ToList();
    public List<WorkflowHistoryResponse> GetWorkflowHistoriesByDeviceId(int deviceId) => _dataAccess.GetWorkflowHistoriesByDeviceId(deviceId).Select(MapToResponse).ToList();

    public WorkflowHistoryResponse CreateWorkflowHistory(CreateWorkflowHistoryRequest request)
    {
        Validate(request.WorkflowId, request.DeviceId, request.ProviderType, request.Status, request.Success, request.StartedAtUtc, request.CompletedAtUtc, request.FailedStepNumber, request.RollbackOfWorkflowHistoryId);
        var model = new WorkflowHistory
        {
            WorkflowId = request.WorkflowId,
            DeviceId = request.DeviceId,
            ProviderType = request.ProviderType,
            Status = request.Status,
            Executed = request.Executed,
            Success = request.Success,
            StartedAtUtc = request.StartedAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
            FailedStepNumber = request.FailedStepNumber,
            FailureMessage = request.FailureMessage,
            RollbackOfWorkflowHistoryId = request.RollbackOfWorkflowHistoryId,
            CreatedDate = DateTime.UtcNow
        };
        return MapToResponse(_dataAccess.InsertWorkflowHistory(model));
    }

    public WorkflowHistoryResponse CreateExternalWorkflowHistory(CreateExternalWorkflowHistoryRequest request)
    {
        return CreateWorkflowHistory(new CreateWorkflowHistoryRequest
        {
            WorkflowId = request.WorkflowId,
            DeviceId = request.DeviceId,
            ProviderType = request.ProviderType,
            Status = request.Status,
            Executed = request.Executed,
            Success = request.Success,
            StartedAtUtc = request.StartedAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
            FailedStepNumber = request.FailedStepNumber,
            FailureMessage = request.FailureMessage,
            RollbackOfWorkflowHistoryId = request.RollbackOfWorkflowHistoryId
        });
    }

    private void Validate(int? workflowId, int deviceId, string providerType, string status, bool success, DateTime? startedAtUtc, DateTime? completedAtUtc, int? failedStepNumber, int? rollbackOfWorkflowHistoryId)
    {
        if (deviceId <= 0) throw new ArgumentException("DeviceId is required.");
        if (!_dataAccess.DeviceExists(deviceId)) throw new ArgumentException("Device must exist.");
        if (!DomainConstants.IsProviderType(providerType)) throw new ArgumentException("ProviderType must be Api, Console, File, Poll, or Rollback.");
        if (!DomainConstants.IsWorkStatus(status)) throw new ArgumentException("Status must be Queued, Claimed, Executing, Completed, Failed, Cancelled, Expired, or RolledBack.");
        if (workflowId != null && !_dataAccess.WorkflowExists(workflowId.Value)) throw new ArgumentException("WorkflowId must reference an existing workflow.");
        if (failedStepNumber != null && failedStepNumber <= 0) throw new ArgumentException("FailedStepNumber must be greater than zero when supplied.");
        if (failedStepNumber != null && success && !status.Equals("Failed", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("FailedStepNumber should only be set when Success is false or Status is Failed.");
        if (startedAtUtc != null && completedAtUtc != null && completedAtUtc < startedAtUtc) throw new ArgumentException("CompletedAtUtc cannot be earlier than StartedAtUtc.");
        if (rollbackOfWorkflowHistoryId != null && _dataAccess.GetWorkflowHistoryById(rollbackOfWorkflowHistoryId.Value) == null) throw new ArgumentException("RollbackOfWorkflowHistoryId must reference an existing workflow history row.");
    }

    public static WorkflowHistoryResponse MapToResponse(WorkflowHistory model) => new()
    {
        Id = model.Id,
        WorkflowId = model.WorkflowId,
        DeviceId = model.DeviceId,
        ProviderType = model.ProviderType,
        Status = model.Status,
        Executed = model.Executed,
        Success = model.Success,
        StartedAtUtc = model.StartedAtUtc,
        CompletedAtUtc = model.CompletedAtUtc,
        FailedStepNumber = model.FailedStepNumber,
        FailureMessage = model.FailureMessage,
        RollbackOfWorkflowHistoryId = model.RollbackOfWorkflowHistoryId,
        CreatedDate = model.CreatedDate
    };
}
