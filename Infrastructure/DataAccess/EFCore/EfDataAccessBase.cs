using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public abstract class EfDataAccessBase
{
    protected readonly RobotContext Context;

    protected EfDataAccessBase(RobotContext context)
    {
        Context = context;
    }

    protected object? Scalar(string sql, NpgsqlParameter[]? parameters = null)
    {
        using var connection = new NpgsqlConnection(Context.Database.GetConnectionString());
        using var command = new NpgsqlCommand(sql, connection);
        if (parameters != null) command.Parameters.AddRange(parameters);
        connection.Open();
        return command.ExecuteScalar();
    }

    protected bool Exists(string sql, NpgsqlParameter[]? parameters = null) => Convert.ToBoolean(Scalar(sql, parameters));

    protected int? NullableInt(string sql, NpgsqlParameter[]? parameters = null)
    {
        var value = Scalar(sql, parameters);
        return value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
    }

    protected string? NullableString(string sql, NpgsqlParameter[]? parameters = null)
    {
        var value = Scalar(sql, parameters);
        return value == null || value == DBNull.Value ? null : Convert.ToString(value);
    }
}
