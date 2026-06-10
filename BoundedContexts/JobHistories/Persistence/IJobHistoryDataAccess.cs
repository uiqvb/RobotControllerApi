using RobotControllerApi.BoundedContexts.JobHistories.Models;

namespace RobotControllerApi.BoundedContexts.JobHistories.Persistence;

public interface IJobHistoryDataAccess
{
    List<JobHistory> GetJobHistories();
    JobHistory? GetJobHistoryById(int id);
    List<JobHistory> GetJobHistoriesByJobId(int jobId);
    List<JobHistory> GetJobHistoriesByWorkflowId(int workflowId);
    List<JobHistory> GetJobHistoriesByDeviceId(int deviceId);
    JobHistory InsertJobHistory(JobHistory newJobHistory);
    bool DeviceExists(int deviceId);
    bool JobExists(int jobId);
    bool WorkflowExists(int workflowId);
    bool CommandCatalogueExists(int commandCatalogueId);
}
