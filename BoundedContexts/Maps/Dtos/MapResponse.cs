namespace RobotControllerApi.BoundedContexts.Maps.Dtos;

public class MapResponse
{
    public int Id { get; set; } = 0;
    public string Name { get; set; } = string.Empty;
    public int Columns { get; set; } = 0;
    public int Rows { get; set; } = 0;
    public double CellSizeCm { get; set; } = 0;
    public string? Description { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = default;
    public DateTime ModifiedDate { get; set; } = default;
}
