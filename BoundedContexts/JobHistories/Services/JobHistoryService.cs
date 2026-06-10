using RobotControllerApi.BoundedContexts.JobHistories.Dtos;
using RobotControllerApi.BoundedContexts.JobHistories.Models;
using RobotControllerApi.BoundedContexts.JobHistories.Persistence;
using RobotControllerApi.BoundedContexts.Jobs.Services;
using RobotControllerApi.BoundedContexts.Shared;

namespace RobotControllerApi.BoundedContexts.JobHistories.Services;

public class JobHistoryService : IJobHistoryService
{
    private static readonly string[] ExecutionKinds = { "Grid", "Continuous", "Mode", "Query" };
    private static readonly string[] RollbackKinds = { "Exact", "BestEffort", "None" };

    private readonly IJobHistoryDataAccess _dataAccess;

    public JobHistoryService(IJobHistoryDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public List<JobHistoryResponse> GetJobHistories() => _dataAccess.GetJobHistories().Select(MapToResponse).ToList();
    public JobHistoryResponse? GetJobHistoryById(int id) => _dataAccess.GetJobHistoryById(id) is { } model ? MapToResponse(model) : null;
    public List<JobHistoryResponse> GetJobHistoriesByJobId(int jobId) => _dataAccess.GetJobHistoriesByJobId(jobId).Select(MapToResponse).ToList();
    public List<JobHistoryResponse> GetJobHistoriesByWorkflowId(int workflowId) => _dataAccess.GetJobHistoriesByWorkflowId(workflowId).Select(MapToResponse).ToList();
    public List<JobHistoryResponse> GetJobHistoriesByDeviceId(int deviceId) => _dataAccess.GetJobHistoriesByDeviceId(deviceId).Select(MapToResponse).ToList();

    public JobHistoryResponse CreateJobHistory(CreateJobHistoryRequest request)
    {
        Validate(request);

        var startedAtUtc = request.StartedAtUtc;
        var completedAtUtc = request.CompletedAtUtc;
        var durationMs = request.DurationMs;

        if (durationMs == null && startedAtUtc.HasValue && completedAtUtc.HasValue)
        {
            durationMs = Math.Max(0, (int)Math.Round((completedAtUtc.Value - startedAtUtc.Value).TotalMilliseconds));
        }

        var model = new JobHistory
        {
            JobId = request.JobId,
            WorkflowId = request.WorkflowId,
            StepNumber = request.StepNumber,
            DeviceId = request.DeviceId,
            CommandCatalogueId = request.CommandCatalogueId,
            CommandName = request.CommandName.Trim(),
            PayloadJson = string.IsNullOrWhiteSpace(request.PayloadJson) ? "{}" : request.PayloadJson,
            ProviderType = request.ProviderType.Trim(),
            ExecutionKind = NormalizeExecutionKind(request.ExecutionKind),
            RollbackKind = NormalizeRollbackKind(request.RollbackKind),
            Executed = request.Executed,
            Success = request.Success,
            ResultJson = string.IsNullOrWhiteSpace(request.ResultJson) ? null : request.ResultJson,
            FailureCode = string.IsNullOrWhiteSpace(request.FailureCode) ? null : request.FailureCode.Trim(),
            FailureMessage = string.IsNullOrWhiteSpace(request.FailureMessage) ? null : request.FailureMessage.Trim(),
            StartedAtUtc = startedAtUtc,
            CompletedAtUtc = completedAtUtc,
            DurationMs = durationMs,
            RollbackOfJobHistoryId = request.RollbackOfJobHistoryId,
            CreatedDate = DateTime.UtcNow
        };

        return MapToResponse(_dataAccess.InsertJobHistory(model));
    }

    public JobHistoryResponse CreateExternalJobHistory(CreateExternalJobHistoryRequest request)
    {
        return CreateJobHistory(new CreateJobHistoryRequest
        {
            JobId = null,
            WorkflowId = request.WorkflowId,
            StepNumber = request.StepNumber,
            DeviceId = request.DeviceId,
            CommandCatalogueId = request.CommandCatalogueId,
            CommandName = request.CommandName,
            PayloadJson = request.PayloadJson,
            ProviderType = request.ProviderType,
            ExecutionKind = request.ExecutionKind,
            RollbackKind = request.RollbackKind,
            Executed = request.Executed,
            Success = request.Success,
            ResultJson = request.ResultJson,
            FailureCode = request.FailureCode,
            FailureMessage = request.FailureMessage,
            StartedAtUtc = request.StartedAtUtc,
            CompletedAtUtc = request.CompletedAtUtc,
            DurationMs = request.DurationMs,
            RollbackOfJobHistoryId = request.RollbackOfJobHistoryId
        });
    }

    private void Validate(CreateJobHistoryRequest request)
    {
        if (request.DeviceId <= 0) throw new ArgumentException("DeviceId is required.");
        if (!_dataAccess.DeviceExists(request.DeviceId)) throw new ArgumentException("Device must exist.");
        if (string.IsNullOrWhiteSpace(request.CommandName)) throw new ArgumentException("CommandName is required.");
        if (request.CommandName.Length > 100) throw new ArgumentException("CommandName cannot exceed 100 characters.");
        if (!DomainConstants.IsProviderType(request.ProviderType)) throw new ArgumentException("ProviderType must be Api, Console, File, Poll, or Rollback.");
        if (!ExecutionKinds.Contains(request.ExecutionKind, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("ExecutionKind must be Grid, Continuous, Mode, or Query.");
        if (!RollbackKinds.Contains(request.RollbackKind, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("RollbackKind must be Exact, BestEffort, or None.");
        if (request.StepNumber != null && request.StepNumber <= 0) throw new ArgumentException("StepNumber must be greater than zero when supplied.");
        if (request.JobId != null && !_dataAccess.JobExists(request.JobId.Value)) throw new ArgumentException("JobId must reference an existing job.");
        if (request.WorkflowId != null && !_dataAccess.WorkflowExists(request.WorkflowId.Value)) throw new ArgumentException("WorkflowId must reference an existing workflow.");
        if (request.CommandCatalogueId != null && !_dataAccess.CommandCatalogueExists(request.CommandCatalogueId.Value)) throw new ArgumentException("CommandCatalogueId must reference an existing command catalogue row.");
        if (request.RollbackOfJobHistoryId != null && _dataAccess.GetJobHistoryById(request.RollbackOfJobHistoryId.Value) == null) throw new ArgumentException("RollbackOfJobHistoryId must reference an existing job history row.");
        if (request.DurationMs != null && request.DurationMs < 0) throw new ArgumentException("DurationMs cannot be negative.");
        if (request.StartedAtUtc.HasValue && request.CompletedAtUtc.HasValue && request.CompletedAtUtc.Value < request.StartedAtUtc.Value) throw new ArgumentException("CompletedAtUtc cannot be before StartedAtUtc.");

        JobService.ValidateJsonObject(request.PayloadJson, "PayloadJson");
        if (!string.IsNullOrWhiteSpace(request.ResultJson)) JobService.ValidateJsonObject(request.ResultJson, "ResultJson");
    }

    private static string NormalizeExecutionKind(string value)
    {
        var match = ExecutionKinds.FirstOrDefault(x => x.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? "Mode";
    }

    private static string NormalizeRollbackKind(string value)
    {
        var match = RollbackKinds.FirstOrDefault(x => x.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? "None";
    }

    public static JobHistoryResponse MapToResponse(JobHistory model) => new()
    {
        Id = model.Id,
        JobId = model.JobId,
        WorkflowId = model.WorkflowId,
        StepNumber = model.StepNumber,
        DeviceId = model.DeviceId,
        CommandCatalogueId = model.CommandCatalogueId,
        CommandName = model.CommandName,
        PayloadJson = model.PayloadJson,
        ProviderType = model.ProviderType,
        ExecutionKind = model.ExecutionKind,
        RollbackKind = model.RollbackKind,
        Executed = model.Executed,
        Success = model.Success,
        ResultJson = model.ResultJson,
        FailureCode = model.FailureCode,
        FailureMessage = model.FailureMessage,
        StartedAtUtc = model.StartedAtUtc,
        CompletedAtUtc = model.CompletedAtUtc,
        DurationMs = model.DurationMs,
        RollbackOfJobHistoryId = model.RollbackOfJobHistoryId,
        CreatedDate = model.CreatedDate
    };
}
