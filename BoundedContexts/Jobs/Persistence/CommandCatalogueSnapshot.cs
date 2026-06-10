namespace RobotControllerApi.BoundedContexts.Jobs.Persistence;

public class CommandCatalogueSnapshot
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExecutionKind { get; set; } = "Mode";
    public string RollbackKind { get; set; } = "None";
    public string? InverseCommandName { get; set; }
    public bool RequiresDuration { get; set; }
    public bool IsActive { get; set; }
}
