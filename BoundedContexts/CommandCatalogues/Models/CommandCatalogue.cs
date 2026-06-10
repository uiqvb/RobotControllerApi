namespace RobotControllerApi.BoundedContexts.CommandCatalogues.Models;

public class CommandCatalogue
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = null;

    // Grid: map/grid command such as MOVE, LEFT, RIGHT, STEP_BACK.
    // Continuous: timed or live movement command such as MOVE_FORWARD, ROTATE_LEFT.
    // Mode: command that changes runtime mode/state such as STOP, AUTO, SET_MODE_FAST.
    // Query: command that asks for information such as REPORT.
    public string ExecutionKind { get; set; } = "Mode";

    // Exact: logical inverse is trusted, usually grid commands.
    // BestEffort: inverse can be generated, but physical reversal is approximate.
    // None: no rollback/compensation should be generated.
    public string RollbackKind { get; set; } = "None";

    public string? InverseCommandName { get; set; } = null;
    public bool RequiresDuration { get; set; } = false;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}
