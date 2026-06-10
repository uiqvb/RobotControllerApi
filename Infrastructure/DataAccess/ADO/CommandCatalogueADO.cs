using Npgsql;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class CommandCatalogueADO : ICommandCatalogueDataAccess
{
    private readonly DbConfig _dbConfig;

    public CommandCatalogueADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public List<CommandCatalogue> GetCommandCatalogues()
    {
        var commandCatalogues = new List<CommandCatalogue>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              ORDER BY id;", conn);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            commandCatalogues.Add(MapCommandCatalogue(dr));
        }

        return commandCatalogues;
    }

    public CommandCatalogue? GetCommandCatalogueById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate
              FROM public.commandcatalogue
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        using var dr = cmd.ExecuteReader();

        return dr.Read() ? MapCommandCatalogue(dr) : null;
    }

    public bool CommandCatalogueExistsByName(string name, int? excludeId = null)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        var sql = @"SELECT id
                    FROM public.commandcatalogue
                    WHERE UPPER(name) = UPPER(@name)";

        if (excludeId.HasValue)
        {
            sql += " AND id <> @excludeId";
        }

        using var cmd = new NpgsqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("name", name);

        if (excludeId.HasValue)
        {
            cmd.Parameters.AddWithValue("excludeId", excludeId.Value);
        }

        using var dr = cmd.ExecuteReader();
        return dr.Read();
    }

    public CommandCatalogue InsertCommandCatalogue(CommandCatalogue newCommandCatalogue)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.commandcatalogue
              (name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate)
              VALUES (@name, @description, @executionKind, @rollbackKind, @inverseCommandName, @requiresDuration, @isActive, @createdDate, @modifiedDate)
              RETURNING id, name, description, executionkind, rollbackkind, inversecommandname, requiresduration, isactive, createddate, modifieddate;", conn);

        AddParameters(cmd, newCommandCatalogue);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapCommandCatalogue(dr);
        }

        throw new InvalidOperationException("Command catalogue insert failed.");
    }

    public bool UpdateCommandCatalogue(int id, CommandCatalogue updatedCommandCatalogue)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.commandcatalogue
              SET name = @name,
                  description = @description,
                  executionkind = @executionKind,
                  rollbackkind = @rollbackKind,
                  inversecommandname = @inverseCommandName,
                  requiresduration = @requiresDuration,
                  isactive = @isActive,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        AddParameters(cmd, updatedCommandCatalogue);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteCommandCatalogue(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"DELETE FROM public.commandcatalogue
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    private static void AddParameters(NpgsqlCommand cmd, CommandCatalogue model)
    {
        cmd.Parameters.AddWithValue("name", model.Name);
        cmd.Parameters.AddWithValue("description", model.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("executionKind", model.ExecutionKind);
        cmd.Parameters.AddWithValue("rollbackKind", model.RollbackKind);
        cmd.Parameters.AddWithValue("inverseCommandName", model.InverseCommandName ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("requiresDuration", model.RequiresDuration);
        cmd.Parameters.AddWithValue("isActive", model.IsActive);
        cmd.Parameters.AddWithValue("createdDate", model.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", model.ModifiedDate);
    }

    private static CommandCatalogue MapCommandCatalogue(NpgsqlDataReader dr)
    {
        return new CommandCatalogue
        {
            Id = dr.GetInt32(0),
            Name = dr.GetString(1),
            Description = dr.IsDBNull(2) ? null : dr.GetString(2),
            ExecutionKind = dr.GetString(3),
            RollbackKind = dr.GetString(4),
            InverseCommandName = dr.IsDBNull(5) ? null : dr.GetString(5),
            RequiresDuration = dr.GetBoolean(6),
            IsActive = dr.GetBoolean(7),
            CreatedDate = dr.GetDateTime(8),
            ModifiedDate = dr.GetDateTime(9)
        };
    }
}
