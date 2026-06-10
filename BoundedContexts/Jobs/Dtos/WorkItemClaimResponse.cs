using RobotControllerApi.BoundedContexts.Workflows.Dtos;

namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

public class WorkItemClaimResponse
{
    public string WorkItemType { get; set; } = string.Empty;
    public JobResponse? Job { get; set; }
    public WorkflowWithJobsResponse? Workflow { get; set; }
}
