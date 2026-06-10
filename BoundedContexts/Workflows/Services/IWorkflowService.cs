using RobotControllerApi.BoundedContexts.Workflows.Dtos;

namespace RobotControllerApi.BoundedContexts.Workflows.Services;

public interface IWorkflowService
{
    List<WorkflowResponse> GetWorkflows();
    WorkflowResponse? GetWorkflowById(int id);
    WorkflowWithJobsResponse? GetWorkflowWithJobsById(int id);
    List<WorkflowResponse> GetWorkflowsByDeviceId(int deviceId);
    WorkflowWithJobsResponse CreateWorkflow(int deviceId, CreateWorkflowRequest request, int? requestedByAppUserId = null);
    bool UpdateWorkflow(int id, UpdateWorkflowRequest request);
    bool DeleteWorkflow(int id);
    bool CancelWorkflow(int id);
    bool MarkWorkflowStarted(int id, int? deviceCredentialId = null);
    bool MarkWorkflowCompleted(int id, CompleteWorkflowRequest request, int? deviceCredentialId = null);
    bool MarkWorkflowFailed(int id, FailWorkflowRequest request, int? deviceCredentialId = null);
}
