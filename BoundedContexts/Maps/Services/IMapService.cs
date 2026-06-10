using RobotControllerApi.BoundedContexts.Maps.Dtos;

namespace RobotControllerApi.BoundedContexts.Maps.Services;

public interface IMapService
{
    List<MapResponse> GetMaps();
    MapResponse? GetMapById(int id);
    MapResponse CreateMap(CreateMapRequest request);
    bool UpdateMap(int id, UpdateMapRequest request);
    bool DeleteMap(int id);
    bool DeactivateMap(int id);
    bool ReactivateMap(int id);
}
