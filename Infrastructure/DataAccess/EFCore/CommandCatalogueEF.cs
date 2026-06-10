using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class CommandCatalogueEF : ICommandCatalogueDataAccess
{
    private readonly RobotContext _context;

    public CommandCatalogueEF(RobotContext context)
    {
        _context = context;
    }

    public List<CommandCatalogue> GetCommandCatalogues()
    {
        return _context.CommandCatalogues
            .OrderBy(x => x.Id)
            .ToList();
    }

    public CommandCatalogue? GetCommandCatalogueById(int id)
    {
        return _context.CommandCatalogues
            .SingleOrDefault(x => x.Id == id);
    }

    public bool CommandCatalogueExistsByName(string name, int? excludeId = null)
    {
        var query = _context.CommandCatalogues
            .Where(x => x.Name.ToUpper() == name.ToUpper());

        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return query.Any();
    }

    public CommandCatalogue InsertCommandCatalogue(CommandCatalogue newCommandCatalogue)
    {
        _context.CommandCatalogues.Add(newCommandCatalogue);
        _context.SaveChanges();
        return newCommandCatalogue;
    }

    public bool UpdateCommandCatalogue(int id, CommandCatalogue updatedCommandCatalogue)
    {
        var existingCommandCatalogue = _context.CommandCatalogues
            .SingleOrDefault(x => x.Id == id);

        if (existingCommandCatalogue == null)
        {
            return false;
        }

        existingCommandCatalogue.Name = updatedCommandCatalogue.Name;
        existingCommandCatalogue.Description = updatedCommandCatalogue.Description;
        existingCommandCatalogue.ExecutionKind = updatedCommandCatalogue.ExecutionKind;
        existingCommandCatalogue.RollbackKind = updatedCommandCatalogue.RollbackKind;
        existingCommandCatalogue.InverseCommandName = updatedCommandCatalogue.InverseCommandName;
        existingCommandCatalogue.RequiresDuration = updatedCommandCatalogue.RequiresDuration;
        existingCommandCatalogue.IsActive = updatedCommandCatalogue.IsActive;
        existingCommandCatalogue.ModifiedDate = updatedCommandCatalogue.ModifiedDate;

        _context.SaveChanges();
        return true;
    }

    public bool DeleteCommandCatalogue(int id)
    {
        var existingCommandCatalogue = _context.CommandCatalogues
            .SingleOrDefault(x => x.Id == id);

        if (existingCommandCatalogue == null)
        {
            return false;
        }

        _context.CommandCatalogues.Remove(existingCommandCatalogue);
        _context.SaveChanges();
        return true;
    }
}
