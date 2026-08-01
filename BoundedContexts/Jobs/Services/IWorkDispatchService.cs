using RobotControllerApi.BoundedContexts.Jobs.Dtos;

namespace RobotControllerApi.BoundedContexts.Jobs.Services;

public interface IWorkDispatchService
{
    WorkItemClaimResponse? ClaimNextWorkItem(int deviceId, ClaimJobRequest request, int deviceCredentialId);

    // Replays a robot's offline rollback so the backend's pose matches physical reality again.
    OfflineRollbackReconcileResponse ReportOfflineRollback(int deviceId, ReportOfflineRollbackRequest request, int deviceCredentialId);

    // Drains leases that expired while nobody was polling. Returns the number of jobs settled.
    int ExpireStaleWorkForAllDevices();
}
