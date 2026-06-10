using RobotControllerApi.BoundedContexts.Jobs.Dtos;

namespace RobotControllerApi.BoundedContexts.Jobs.Services;

public interface IWorkDispatchService
{
    WorkItemClaimResponse? ClaimNextWorkItem(int deviceId, ClaimJobRequest request, int deviceCredentialId);
}
