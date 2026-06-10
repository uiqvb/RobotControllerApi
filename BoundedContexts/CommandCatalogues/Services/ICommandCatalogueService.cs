using RobotControllerApi.BoundedContexts.CommandCatalogues.Dtos;

namespace RobotControllerApi.BoundedContexts.CommandCatalogues.Services;

public interface ICommandCatalogueService
{
    List<CommandCatalogueResponse> GetCommandCatalogues();
    CommandCatalogueResponse? GetCommandCatalogueById(int id);
    CommandCatalogueResponse CreateCommandCatalogue(CreateCommandCatalogueRequest request);
    bool UpdateCommandCatalogue(int id, UpdateCommandCatalogueRequest request);
    bool DeleteCommandCatalogue(int id);
    bool DeactivateCommandCatalogue(int id);
    bool ReactivateCommandCatalogue(int id);
}
