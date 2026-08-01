namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

// What the backend made of the robot's offline rollback report: where it now believes the
// robot is, and whether it trusts that enough to dispatch grid work again.
public class OfflineRollbackReconcileResponse
{
    public int StepsAccepted { get; set; }
    public int StepsRejected { get; set; }

    public int? GridX { get; set; }
    public int? GridY { get; set; }
    public string? Facing { get; set; }

    // False means reconciliation could not land on a pose worth trusting; the robot needs a
    // fresh PLACE before it will be given grid work again.
    public bool IsGridPoseTrusted { get; set; }

    public string Message { get; set; } = string.Empty;
}
