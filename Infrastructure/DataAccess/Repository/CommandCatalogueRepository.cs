using Npgsql;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class CommandCatalogueRepository : ICommandCatalogueDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public CommandCatalogueRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public List<CommandCatalogue> GetCommandCatalogues()
        => _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              ORDER BY id;");

    public CommandCatalogue? GetCommandCatalogueById(int id)
        => _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              WHERE id = @id;",
            new[] { new NpgsqlParameter("id", id) }
        ).SingleOrDefault();

    public bool CommandCatalogueExistsByName(string name, int? excludeId = null)
        => GetCommandCatalogues().Any(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
            (!excludeId.HasValue || x.Id != excludeId.Value));

    public CommandCatalogue InsertCommandCatalogue(CommandCatalogue newCommandCatalogue)
    {
        return _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"INSERT INTO public.commandcatalogue
              (name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate)
              VALUES (@name, @description, @executionKind, @rollbackKind, @inverseCommandName, @requiresDuration, @isActive, @createdDate, @modifiedDate)
              RETURNING id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate;",
            new[]
            {
                new NpgsqlParameter("name", newCommandCatalogue.Name),
                new NpgsqlParameter("description", newCommandCatalogue.Description ?? (object)DBNull.Value),
                new NpgsqlParameter("executionKind", newCommandCatalogue.ExecutionKind),
                new NpgsqlParameter("rollbackKind", newCommandCatalogue.RollbackKind),
                new NpgsqlParameter("inverseCommandName", newCommandCatalogue.InverseCommandName ?? (object)DBNull.Value),
                new NpgsqlParameter("requiresDuration", newCommandCatalogue.RequiresDuration),
                new NpgsqlParameter("isActive", newCommandCatalogue.IsActive),
                new NpgsqlParameter("createdDate", newCommandCatalogue.CreatedDate),
                new NpgsqlParameter("modifiedDate", newCommandCatalogue.ModifiedDate)
            }
        ).Single();
    }

    public bool UpdateCommandCatalogue(int id, CommandCatalogue updatedCommandCatalogue)
    {
        var result = _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"UPDATE public.commandcatalogue
              SET name = @name,
                  description = @description,
                  executionkind = @executionKind,
                  rollbackkind = @rollbackKind,
                  inversecommandname = @inverseCommandName,
                  requiresduration = @requiresDuration,
                  isactive = @isActive,
                  modifieddate = @modifiedDate
              WHERE id = @id
              RETURNING id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate;",
            new[]
            {
                new NpgsqlParameter("id", id),
                new NpgsqlParameter("name", updatedCommandCatalogue.Name),
                new NpgsqlParameter("description", updatedCommandCatalogue.Description ?? (object)DBNull.Value),
                new NpgsqlParameter("executionKind", updatedCommandCatalogue.ExecutionKind),
                new NpgsqlParameter("rollbackKind", updatedCommandCatalogue.RollbackKind),
                new NpgsqlParameter("inverseCommandName", updatedCommandCatalogue.InverseCommandName ?? (object)DBNull.Value),
                new NpgsqlParameter("requiresDuration", updatedCommandCatalogue.RequiresDuration),
                new NpgsqlParameter("isActive", updatedCommandCatalogue.IsActive),
                new NpgsqlParameter("modifiedDate", updatedCommandCatalogue.ModifiedDate)
            }
        );

        return result.Any();
    }

    public bool DeleteCommandCatalogue(int id)
    {
        var result = _repo.ExecuteReader<CommandCatalogue>(
            ConnectionString,
            @"DELETE FROM public.commandcatalogue
              WHERE id = @id
              RETURNING id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate;",
            new[] { new NpgsqlParameter("id", id) }
        );

        return result.Any();
    }
}
