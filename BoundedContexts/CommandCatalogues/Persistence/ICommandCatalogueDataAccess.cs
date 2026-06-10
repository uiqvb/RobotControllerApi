using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;

namespace RobotControllerApi.BoundedContexts.CommandCatalogues.Persistence;

public interface ICommandCatalogueDataAccess
{
    List<CommandCatalogue> GetCommandCatalogues();
    CommandCatalogue? GetCommandCatalogueById(int id);
    bool CommandCatalogueExistsByName(string name, int? excludeId = null);
    CommandCatalogue InsertCommandCatalogue(CommandCatalogue newCommandCatalogue);
    bool UpdateCommandCatalogue(int id, CommandCatalogue updatedCommandCatalogue);
    bool DeleteCommandCatalogue(int id);
}
