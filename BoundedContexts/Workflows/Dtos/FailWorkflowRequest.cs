namespace RobotControllerApi.BoundedContexts.Workflows.Dtos;

public class FailWorkflowRequest
{
    public int? FailedStepNumber { get; set; }
    public string? FailureMessage { get; set; }
}
