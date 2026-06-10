using Npgsql;
using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class AppUserADO : IAppUserDataAccess
{
    private readonly DbConfig _dbConfig;

    public AppUserADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public List<AppUser> GetAppUsers()
    {
        var results = new List<AppUser>();

        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate FROM appuser ORDER BY id", conn);
        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            results.Add(MapFromReader(dr));
        }

        return results;
    }

    public AppUser? GetAppUserById(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate FROM appuser WHERE id = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapFromReader(dr);
        }

        return null;
    }

    public AppUser? GetAppUserByEmail(string email)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate FROM appuser WHERE UPPER(email) = UPPER(@Email)", conn);
        cmd.Parameters.AddWithValue("@Email", email);

        using var dr = cmd.ExecuteReader();

        if (dr.Read())
        {
            return MapFromReader(dr);
        }

        return null;
    }

    public bool AppUserExistsByEmail(string email, int? excludeId = null)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        string sql = excludeId.HasValue 
            ? "SELECT 1 FROM appuser WHERE UPPER(email) = UPPER(@Email) AND id <> @ExcludeId" 
            : "SELECT 1 FROM appuser WHERE UPPER(email) = UPPER(@Email)";

        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Email", email);
        if (excludeId.HasValue)
        {
            cmd.Parameters.AddWithValue("@ExcludeId", excludeId.Value);
        }

        var result = cmd.ExecuteScalar();
        return result != null;
    }

    public AppUser CreateAppUser(AppUser newAppUser)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO appuser
              (email, displayname, role, isactive, lastloginatutc, createddate, modifieddate)
              VALUES (@Email, @DisplayName, @Role, @IsActive, @LastLoginAtUtc, @CreatedDate, @ModifiedDate)
              RETURNING id;", conn);

        cmd.Parameters.AddWithValue("@Email", (object?)newAppUser.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DisplayName", (object?)newAppUser.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Role", (object?)newAppUser.Role ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", (object?)newAppUser.IsActive ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastLoginAtUtc", (object?)newAppUser.LastLoginAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedDate", (object?)newAppUser.CreatedDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ModifiedDate", (object?)newAppUser.ModifiedDate ?? DBNull.Value);

        var id = cmd.ExecuteScalar();
        newAppUser.Id = Convert.ToInt32(id);
        return newAppUser;
    }

    public bool UpdateAppUser(int id, AppUser updatedAppUser)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE appuser
              SET email = @Email, displayname = @DisplayName, role = @Role, isactive = @IsActive, lastloginatutc = @LastLoginAtUtc, createddate = @CreatedDate, modifieddate = @ModifiedDate
              WHERE id = @Id;", conn);

        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Email", (object?)updatedAppUser.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@DisplayName", (object?)updatedAppUser.DisplayName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@Role", (object?)updatedAppUser.Role ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@IsActive", (object?)updatedAppUser.IsActive ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@LastLoginAtUtc", (object?)updatedAppUser.LastLoginAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedDate", (object?)updatedAppUser.CreatedDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ModifiedDate", (object?)updatedAppUser.ModifiedDate ?? DBNull.Value);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteAppUser(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand("DELETE FROM appuser WHERE id = @Id;", conn);
        cmd.Parameters.AddWithValue("@Id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    private static AppUser MapFromReader(NpgsqlDataReader dr)
    {
        return new AppUser
        {
            Id = dr.GetInt32(0),
            Email = dr.GetString(1),
            DisplayName = dr.GetString(2),
            Role = dr.GetString(3),
            IsActive = dr.GetBoolean(4),
            LastLoginAtUtc = dr.IsDBNull(5) ? null : dr.GetDateTime(5),
            CreatedDate = dr.GetDateTime(6),
            ModifiedDate = dr.GetDateTime(7)
        };
    }
}