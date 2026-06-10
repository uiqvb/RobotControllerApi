namespace RobotControllerApi.BoundedContexts.Workflows.Dtos;

public class CreateWorkflowStepRequest
{
    public int StepNumber { get; set; }
    public int CommandCatalogueId { get; set; }
    public string PayloadJson { get; set; } = "{}";
}
