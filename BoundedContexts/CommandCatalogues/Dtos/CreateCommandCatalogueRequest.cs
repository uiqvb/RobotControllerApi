namespace RobotControllerApi.BoundedContexts.CommandCatalogues.Dtos;

public class CreateCommandCatalogueRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;
    public string ExecutionKind { get; set; } = "Mode";
    public string RollbackKind { get; set; } = "None";
    public string? InverseCommandName { get; set; } = null;
    public bool RequiresDuration { get; set; } = false;
    public bool IsActive { get; set; } = true;
}
