using RobotControllerApi.BoundedContexts.WorkflowHistories.Dtos;

namespace RobotControllerApi.BoundedContexts.WorkflowHistories.Services;

public interface IWorkflowHistoryService
{
    List<WorkflowHistoryResponse> GetWorkflowHistories();
    WorkflowHistoryResponse? GetWorkflowHistoryById(int id);
    List<WorkflowHistoryResponse> GetWorkflowHistoriesByWorkflowId(int workflowId);
    List<WorkflowHistoryResponse> GetWorkflowHistoriesByDeviceId(int deviceId);
    WorkflowHistoryResponse CreateWorkflowHistory(CreateWorkflowHistoryRequest request);
    WorkflowHistoryResponse CreateExternalWorkflowHistory(CreateExternalWorkflowHistoryRequest request);
}
