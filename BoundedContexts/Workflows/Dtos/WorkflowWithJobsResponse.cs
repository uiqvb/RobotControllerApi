using RobotControllerApi.BoundedContexts.Jobs.Dtos;

namespace RobotControllerApi.BoundedContexts.Workflows.Dtos;

public class WorkflowWithJobsResponse : WorkflowResponse
{
    public List<JobResponse> Jobs { get; set; } = new();
}
