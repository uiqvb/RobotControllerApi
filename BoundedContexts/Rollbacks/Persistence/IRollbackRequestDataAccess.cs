using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.Rollbacks.Models;

namespace RobotControllerApi.BoundedContexts.Rollbacks.Persistence;

public interface IRollbackRequestDataAccess
{
    List<RollbackRequest> GetRollbackRequests();
    RollbackRequest? GetRollbackRequestById(int id);
    List<RollbackRequest> GetRollbackRequestsByDeviceId(int deviceId);
    RollbackRequest InsertRollbackRequest(RollbackRequest newRollbackRequest);
    bool UpdateRollbackRequest(int id, RollbackRequest updatedRollbackRequest);
    bool DeviceExistsAndActive(int deviceId);
    bool HasGeneratedRollbackForTarget(string targetType, string targetIdsJson);
    CommandCatalogue? GetActiveCommandCatalogueById(int id);
    CommandCatalogue? GetActiveCommandCatalogueByName(string name);
}
