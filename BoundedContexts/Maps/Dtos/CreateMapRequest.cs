namespace RobotControllerApi.BoundedContexts.Maps.Dtos;

public class CreateMapRequest
{
    public string Name { get; set; } = string.Empty;
    public int Columns { get; set; }
    public int Rows { get; set; }
    public double CellSizeCm { get; set; }
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
}
