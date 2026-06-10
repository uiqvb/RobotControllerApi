using RobotControllerApi.BoundedContexts.Jobs.Models;

namespace RobotControllerApi.BoundedContexts.Jobs.Persistence;

public interface IJobDataAccess
{
    List<Job> GetJobs();
    Job? GetJobById(int id);
    List<Job> GetJobsByDeviceId(int deviceId);
    List<Job> GetJobsByWorkflowId(int workflowId);
    Job? GetOldestQueuedStandaloneJobByDeviceId(int deviceId);
    Job? GetOldestQueuedJobByDeviceId(int deviceId);
    Job InsertJob(Job newJob);
    bool UpdateJob(int id, Job updatedJob);
    bool DeleteJob(int id);
    bool DeviceExistsAndActive(int deviceId);
    int? GetDeviceMapId(int deviceId);
    bool CommandCatalogueExistsAndActive(int commandCatalogueId);
    string? GetCommandCatalogueNameById(int commandCatalogueId);
    CommandCatalogueSnapshot? GetCommandCatalogueById(int commandCatalogueId);
    int? GetCommandCatalogueIdByName(string commandName);
    DeviceCapabilitySnapshot? GetActiveDeviceCapability(int deviceId, int commandCatalogueId);
    bool IsDeviceGridPoseTrustedAndAligned(int deviceId);
    bool DeviceCredentialOwnsDevice(int deviceCredentialId, int deviceId);
}
