using Npgsql;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public interface IRepository
{
    public List<T> ExecuteReader<T>(
        string connectionString,
        string sqlCommand,
        NpgsqlParameter[]? dbParams = null)
        where T : class, new()
    {
        var entities = new List<T>();

        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(sqlCommand, conn);

        if (dbParams is not null)
        {
            cmd.Parameters.AddRange(dbParams);
        }

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            var entity = new T();
            dr.MapTo(entity);
            entities.Add(entity);
        }

        return entities;
    }
}
