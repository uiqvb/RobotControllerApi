using RobotControllerApi.BoundedContexts.CommandCatalogues.Dtos;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Models;
using RobotControllerApi.BoundedContexts.CommandCatalogues.Persistence;

namespace RobotControllerApi.BoundedContexts.CommandCatalogues.Services;

public class CommandCatalogueService : ICommandCatalogueService
{
    private static readonly string[] ExecutionKinds = { "Grid", "Continuous", "Mode", "Query" };
    private static readonly string[] RollbackKinds = { "Exact", "BestEffort", "None" };

    private readonly ICommandCatalogueDataAccess _dataAccess;

    public CommandCatalogueService(ICommandCatalogueDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public List<CommandCatalogueResponse> GetCommandCatalogues()
    {
        return _dataAccess.GetCommandCatalogues()
            .Select(MapToResponse)
            .ToList();
    }

    public CommandCatalogueResponse? GetCommandCatalogueById(int id)
    {
        var commandCatalogue = _dataAccess.GetCommandCatalogueById(id);
        return commandCatalogue == null ? null : MapToResponse(commandCatalogue);
    }

    public CommandCatalogueResponse CreateCommandCatalogue(CreateCommandCatalogueRequest request)
    {
        ValidateRequest(
            request.Name,
            request.Description,
            request.ExecutionKind,
            request.RollbackKind,
            request.InverseCommandName,
            request.RequiresDuration);

        var trimmedName = NormalizeCommandName(request.Name);
        var executionKind = NormalizeExecutionKind(request.ExecutionKind);
        var rollbackKind = NormalizeRollbackKind(request.RollbackKind);
        var inverseCommandName = NormalizeOptionalCommandName(request.InverseCommandName);

        if (_dataAccess.CommandCatalogueExistsByName(trimmedName))
        {
            throw new InvalidOperationException("A command catalogue item with this name already exists.");
        }

        ValidateInverseCommand(trimmedName, rollbackKind, inverseCommandName);

        var now = DateTime.UtcNow;

        var commandCatalogue = new CommandCatalogue
        {
            Name = trimmedName,
            Description = NormalizeOptionalText(request.Description),
            ExecutionKind = executionKind,
            RollbackKind = rollbackKind,
            InverseCommandName = inverseCommandName,
            RequiresDuration = request.RequiresDuration,
            IsActive = request.IsActive,
            CreatedDate = now,
            ModifiedDate = now
        };

        var savedCommandCatalogue = _dataAccess.InsertCommandCatalogue(commandCatalogue);
        return MapToResponse(savedCommandCatalogue);
    }

    public bool UpdateCommandCatalogue(int id, UpdateCommandCatalogueRequest request)
    {
        var existingCommandCatalogue = _dataAccess.GetCommandCatalogueById(id);

        if (existingCommandCatalogue == null)
        {
            return false;
        }

        ValidateRequest(
            request.Name,
            request.Description,
            request.ExecutionKind,
            request.RollbackKind,
            request.InverseCommandName,
            request.RequiresDuration);

        var trimmedName = NormalizeCommandName(request.Name);
        var executionKind = NormalizeExecutionKind(request.ExecutionKind);
        var rollbackKind = NormalizeRollbackKind(request.RollbackKind);
        var inverseCommandName = NormalizeOptionalCommandName(request.InverseCommandName);

        if (_dataAccess.CommandCatalogueExistsByName(trimmedName, id))
        {
            throw new InvalidOperationException("A command catalogue item with this name already exists.");
        }

        ValidateInverseCommand(trimmedName, rollbackKind, inverseCommandName);

        var commandCatalogueToUpdate = new CommandCatalogue
        {
            Id = id,
            Name = trimmedName,
            Description = NormalizeOptionalText(request.Description),
            ExecutionKind = executionKind,
            RollbackKind = rollbackKind,
            InverseCommandName = inverseCommandName,
            RequiresDuration = request.RequiresDuration,
            IsActive = request.IsActive,
            CreatedDate = existingCommandCatalogue.CreatedDate,
            ModifiedDate = DateTime.UtcNow
        };

        return _dataAccess.UpdateCommandCatalogue(id, commandCatalogueToUpdate);
    }

    public bool DeleteCommandCatalogue(int id)
    {
        return _dataAccess.DeleteCommandCatalogue(id);
    }

    public bool DeactivateCommandCatalogue(int id)
    {
        var existingCommandCatalogue = _dataAccess.GetCommandCatalogueById(id);

        if (existingCommandCatalogue == null)
        {
            return false;
        }

        existingCommandCatalogue.IsActive = false;
        existingCommandCatalogue.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateCommandCatalogue(id, existingCommandCatalogue);
    }

    public bool ReactivateCommandCatalogue(int id)
    {
        var existingCommandCatalogue = _dataAccess.GetCommandCatalogueById(id);

        if (existingCommandCatalogue == null)
        {
            return false;
        }

        existingCommandCatalogue.IsActive = true;
        existingCommandCatalogue.ModifiedDate = DateTime.UtcNow;

        return _dataAccess.UpdateCommandCatalogue(id, existingCommandCatalogue);
    }

    private static void ValidateRequest(
        string name,
        string? description,
        string executionKind,
        string rollbackKind,
        string? inverseCommandName,
        bool requiresDuration)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.");
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException("Name cannot exceed 100 characters.");
        }

        if (description != null && description.Length > 1000)
        {
            throw new ArgumentException("Description cannot exceed 1000 characters.");
        }

        if (!ExecutionKinds.Contains(executionKind, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("ExecutionKind must be Grid, Continuous, Mode, or Query.");
        }

        if (!RollbackKinds.Contains(rollbackKind, StringComparer.OrdinalIgnoreCase))
        {
            throw new ArgumentException("RollbackKind must be Exact, BestEffort, or None.");
        }

        var normalizedExecutionKind = NormalizeExecutionKind(executionKind);
        var normalizedRollbackKind = NormalizeRollbackKind(rollbackKind);

        if (normalizedRollbackKind != "None" && string.IsNullOrWhiteSpace(inverseCommandName))
        {
            throw new ArgumentException("InverseCommandName is required when RollbackKind is Exact or BestEffort.");
        }

        if (normalizedRollbackKind == "None" && !string.IsNullOrWhiteSpace(inverseCommandName))
        {
            throw new ArgumentException("InverseCommandName must be empty when RollbackKind is None.");
        }

        if (normalizedExecutionKind == "Query" && normalizedRollbackKind != "None")
        {
            throw new ArgumentException("Query commands cannot be rollbackable.");
        }

        if (normalizedExecutionKind == "Query" && requiresDuration)
        {
            throw new ArgumentException("Query commands cannot require duration.");
        }

        if (normalizedExecutionKind != "Continuous" && requiresDuration)
        {
            throw new ArgumentException("Only Continuous commands can require duration.");
        }

        if (inverseCommandName != null && inverseCommandName.Trim().Length > 100)
        {
            throw new ArgumentException("InverseCommandName cannot exceed 100 characters.");
        }
    }

    private static void ValidateInverseCommand(string commandName, string rollbackKind, string? inverseCommandName)
    {
        if (rollbackKind == "None")
        {
            return;
        }

        if (string.Equals(commandName, inverseCommandName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("InverseCommandName cannot be the same as Name.");
        }
    }

    private static string NormalizeCommandName(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptionalCommandName(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeExecutionKind(string value)
    {
        var match = ExecutionKinds.SingleOrDefault(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? value.Trim();
    }

    private static string NormalizeRollbackKind(string value)
    {
        var match = RollbackKinds.SingleOrDefault(x => x.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
        return match ?? value.Trim();
    }

    private static CommandCatalogueResponse MapToResponse(CommandCatalogue commandCatalogue)
    {
        return new CommandCatalogueResponse
        {
            Id = commandCatalogue.Id,
            Name = commandCatalogue.Name,
            Description = commandCatalogue.Description,
            ExecutionKind = commandCatalogue.ExecutionKind,
            RollbackKind = commandCatalogue.RollbackKind,
            InverseCommandName = commandCatalogue.InverseCommandName,
            RequiresDuration = commandCatalogue.RequiresDuration,
            IsActive = commandCatalogue.IsActive,
            CreatedDate = commandCatalogue.CreatedDate,
            ModifiedDate = commandCatalogue.ModifiedDate
        };
    }
}
