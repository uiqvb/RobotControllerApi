using RobotControllerApi.BoundedContexts.JobHistories.Dtos;

namespace RobotControllerApi.BoundedContexts.JobHistories.Services;

public interface IJobHistoryService
{
    List<JobHistoryResponse> GetJobHistories();
    JobHistoryResponse? GetJobHistoryById(int id);
    List<JobHistoryResponse> GetJobHistoriesByJobId(int jobId);
    List<JobHistoryResponse> GetJobHistoriesByWorkflowId(int workflowId);
    List<JobHistoryResponse> GetJobHistoriesByDeviceId(int deviceId);
    JobHistoryResponse CreateJobHistory(CreateJobHistoryRequest request);
    JobHistoryResponse CreateExternalJobHistory(CreateExternalJobHistoryRequest request);
}
