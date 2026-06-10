using RobotControllerApi.BoundedContexts.Rollbacks.Dtos;

namespace RobotControllerApi.BoundedContexts.Rollbacks.Services;

public interface IRollbackService
{
    List<RollbackRequestResponse> GetRollbackRequests();

    RollbackRequestResponse? GetRollbackRequestById(int id);

    List<RollbackRequestResponse> GetRollbackRequestsByDeviceId(int deviceId);

    RollbackGenerationResponse GenerateRollbackForJob(
        int jobId,
        CreateRollbackFromJobHistoryRequest request);

    RollbackGenerationResponse GenerateRollbackForWorkflow(
        int workflowId,
        CreateRollbackFromWorkflowHistoryRequest request);

    RollbackGenerationResponse GenerateRollbackFromJobHistories(
        CreateRollbackFromJobHistoryRequest request);

    RollbackGenerationResponse GenerateRollbackFromWorkflowHistories(
        CreateRollbackFromWorkflowHistoryRequest request);
}