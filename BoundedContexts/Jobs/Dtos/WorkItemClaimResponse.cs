using RobotControllerApi.BoundedContexts.Workflows.Dtos;

namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

public class WorkItemClaimResponse
{
    public string WorkItemType { get; set; } = string.Empty;
    public JobResponse? Job { get; set; }
    public WorkflowWithJobsResponse? Workflow { get; set; }

    // The command that undoes the one being handed out, so the robot can push it onto a
    // local undo stack as it executes. Null when the command has no trustworthy inverse
    // (non-Exact rollback kind, no configured inverse, or no payload we can transform) —
    // the robot must then treat this step as a barrier it cannot reverse unaided.
    public string? InverseCommandName { get; set; }
    public string? InversePayloadJson { get; set; }
}
