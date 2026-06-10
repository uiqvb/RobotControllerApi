using RobotControllerApi.BoundedContexts.Maps.Dtos;
using RobotControllerApi.BoundedContexts.Maps.Models;
using RobotControllerApi.BoundedContexts.Maps.Persistence;

namespace RobotControllerApi.BoundedContexts.Maps.Services;

public class MapService : IMapService
{
    private readonly IMapDataAccess _dataAccess;

    public MapService(IMapDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public List<MapResponse> GetMaps()
    {
        return _dataAccess.GetMaps().Select(MapToResponse).ToList();
    }

    public MapResponse? GetMapById(int id)
    {
        var model = _dataAccess.GetMapById(id);
        return model == null ? null : MapToResponse(model);
    }

    public MapResponse CreateMap(CreateMapRequest request)
    {
        ValidateRequest(request.Name, request.Columns, request.Rows, request.CellSizeCm, request.Description);
        EnsureNameUnique(request.Name, null);

        var now = DateTime.UtcNow;
        var model = new Map
        {
            Name = request.Name.Trim(),
            Columns = request.Columns,
            Rows = request.Rows,
            CellSizeCm = request.CellSizeCm,
            Description = request.Description,
            IsActive = request.IsActive,
            CreatedDate = now,
            ModifiedDate = now
        };

        return MapToResponse(_dataAccess.InsertMap(model));
    }

    public bool UpdateMap(int id, UpdateMapRequest request)
    {
        ValidateRequest(request.Name, request.Columns, request.Rows, request.CellSizeCm, request.Description);

        var existing = _dataAccess.GetMapById(id);
        if (existing == null) return false;

        EnsureNameUnique(request.Name, id);

        existing.Name = request.Name.Trim();
        existing.Columns = request.Columns;
        existing.Rows = request.Rows;
        existing.CellSizeCm = request.CellSizeCm;
        existing.Description = request.Description;
        existing.IsActive = request.IsActive;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateMap(id, existing);
    }

    public bool DeleteMap(int id)
    {
        if (_dataAccess.GetMapById(id) == null) return false;
        return _dataAccess.DeleteMap(id);
    }

    public bool DeactivateMap(int id)
    {
        var existing = _dataAccess.GetMapById(id);
        if (existing == null) return false;

        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateMap(id, existing);
    }

    public bool ReactivateMap(int id)
    {
        var existing = _dataAccess.GetMapById(id);
        if (existing == null) return false;

        existing.IsActive = true;
        existing.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateMap(id, existing);
    }

    private void ValidateRequest(string name, int columns, int rows, double cellSizeCm, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (name.Length > 100) throw new ArgumentException("Name cannot exceed 100 characters.");
        if (columns <= 0) throw new ArgumentException("Columns must be greater than zero.");
        if (rows <= 0) throw new ArgumentException("Rows must be greater than zero.");
        if (cellSizeCm < 10) throw new ArgumentException("CellSizeCm must be at least 10.");
        if (description != null && description.Length > 1000) throw new ArgumentException("Description cannot exceed 1000 characters.");
    }

    private void EnsureNameUnique(string name, int? currentId)
    {
        if (_dataAccess.MapExistsByName(name.Trim(), currentId))
        {
            throw new InvalidOperationException("A map with this name already exists.");
        }
    }

    private static MapResponse MapToResponse(Map model)
    {
        return new MapResponse
        {
            Id = model.Id,
            Name = model.Name,
            Columns = model.Columns,
            Rows = model.Rows,
            CellSizeCm = model.CellSizeCm,
            Description = model.Description,
            IsActive = model.IsActive,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }
}
