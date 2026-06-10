namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

public class CreateJobRequest
{
    public int CommandCatalogueId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string ProviderType { get; set; } = "Api";
    public int? RequestedByAppUserId { get; set; }
    public int? WorkflowId { get; set; }
    public int? StepNumber { get; set; }
    public bool IsRollback { get; set; } = false;
    public int? RollbackOfJobHistoryId { get; set; }
}
