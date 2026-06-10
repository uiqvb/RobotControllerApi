namespace RobotControllerApi.BoundedContexts.Rollbacks.Dtos;

public class RollbackGenerationResponse
{
    public RollbackRequestResponse RollbackRequest { get; set; } = new();
    public int? GeneratedJobId { get; set; }
    public int? GeneratedWorkflowId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
