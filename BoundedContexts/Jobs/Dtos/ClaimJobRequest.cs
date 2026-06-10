namespace RobotControllerApi.BoundedContexts.Jobs.Dtos;

public class ClaimJobRequest
{
    public int LeaseMinutes { get; set; } = 5;
}
