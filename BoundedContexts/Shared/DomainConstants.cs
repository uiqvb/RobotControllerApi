namespace RobotControllerApi.BoundedContexts.Shared;

public static class DomainConstants
{
    public static readonly string[] ProviderTypes = { "Api", "Console", "File", "Poll", "Rollback" };
    public static readonly string[] WorkStatuses = { "Queued", "Claimed", "Executing", "Completed", "Failed", "Cancelled", "Expired", "RolledBack" };
    public static readonly string[] ExecutionModes = { "BestEffort", "AllOrNothing" };
    public static readonly string[] RollbackTargetTypes = { "JobHistory", "WorkflowHistory" };
    public static readonly string[] RollbackRequestStatuses = { "Requested", "Generated", "Failed", "Cancelled", "DuplicateRejected" };
    public static readonly string[] LiveControlSessionStatuses = { "Active", "Completed", "Cancelled", "Failed", "Expired" };
    public static readonly string[] LiveControlStopReasons = { "UserReleased", "Timeout", "RainOverride", "ManualStop", "NewSessionStarted", "Cancelled" };
    public static readonly string[] LiveControlCommands = { "MOVE_FORWARD", "MOVE_BACKWARD", "ROTATE_LEFT", "ROTATE_RIGHT", "STOP" };
    public static readonly string[] CommandExecutionKinds = { "Grid", "Continuous", "Mode", "Query" };
    public static readonly string[] CommandRollbackKinds = { "Exact", "BestEffort", "None" };

    // Human roles. Case-sensitive on purpose: the stored Role and the AdminOnly policy's
    // RequireRole("Admin") check are case-sensitive, so validation must match exactly.
    public static readonly string[] Roles = { "Admin", "User" };

    // Per-device permission tiers, lowest -> highest authority.
    // Rank is derived from position in this array (index + 1), so order matters.
    public static readonly string[] PermissionTiers = { "Viewer", "Operator", "Manager", "Owner" };

    private static readonly HashSet<string> GridTrustCommandNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "MOVE", "LEFT", "RIGHT", "STEP_BACK", "JUMP_FORWARD", "JUMP_BACKWARD"
    };

    public static bool IsProviderType(string value) => ProviderTypes.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsWorkStatus(string value) => WorkStatuses.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsExecutionMode(string value) => ExecutionModes.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsRollbackTargetType(string value) => RollbackTargetTypes.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsRollbackRequestStatus(string value) => RollbackRequestStatuses.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsLiveControlSessionStatus(string value) => LiveControlSessionStatuses.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsLiveControlStopReason(string value) => LiveControlStopReasons.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsLiveControlCommand(string value) => LiveControlCommands.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsCommandExecutionKind(string value) => CommandExecutionKinds.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsCommandRollbackKind(string value) => CommandRollbackKinds.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool IsRole(string value) => Roles.Contains(value);
    public static bool IsPermissionTier(string value) => PermissionTiers.Contains(value, StringComparer.OrdinalIgnoreCase);
    public static bool RequiresTrustedGridPose(string commandName) => GridTrustCommandNames.Contains(commandName);

    // Returns 1..N for a known permission tier (higher = more authority), or 0 if unknown.
    public static int GetPermissionTierRank(string permissionLevel)
    {
        for (var i = 0; i < PermissionTiers.Length; i++)
        {
            if (string.Equals(PermissionTiers[i], permissionLevel, StringComparison.OrdinalIgnoreCase))
            {
                return i + 1;
            }
        }

        return 0;
    }
}
