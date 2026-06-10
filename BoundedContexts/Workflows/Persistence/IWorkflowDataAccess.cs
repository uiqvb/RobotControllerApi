using RobotControllerApi.BoundedContexts.Workflows.Models;

namespace RobotControllerApi.BoundedContexts.Workflows.Persistence;

public interface IWorkflowDataAccess
{
    List<Workflow> GetWorkflows();
    Workflow? GetWorkflowById(int id);
    List<Workflow> GetWorkflowsByDeviceId(int deviceId);
    Workflow? GetOldestQueuedWorkflowByDeviceId(int deviceId);
    Workflow InsertWorkflow(Workflow newWorkflow);
    bool UpdateWorkflow(int id, Workflow updatedWorkflow);
    bool DeleteWorkflow(int id);
    bool DeviceExistsAndActive(int deviceId);
    bool DeviceCredentialOwnsDevice(int deviceCredentialId, int deviceId);
}
