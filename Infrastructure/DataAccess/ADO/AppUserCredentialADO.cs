using Npgsql;
using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.ADO;

public class AppUserCredentialADO : IAppUserCredentialDataAccess
{
    private readonly DbConfig _dbConfig;

    public AppUserCredentialADO(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    public AppUserCredential? GetCredentialByAppUserId(int appUserId)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"SELECT id, appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate
              FROM public.appusercredential
              WHERE appuserid = @appUserId;", conn);

        cmd.Parameters.AddWithValue("appUserId", appUserId);

        using var dr = cmd.ExecuteReader();
        return dr.Read() ? MapFromReader(dr) : null;
    }

    public AppUserCredential InsertCredential(AppUserCredential newCredential)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO public.appusercredential
              (appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate)
              VALUES (@appUserId, @passwordHash, @passwordHashAlgorithm, @passwordChangedAtUtc, @mustResetPassword, @failedLoginCount, @lockedOutUntilUtc, @createdDate, @modifiedDate)
              RETURNING id, appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate;", conn);

        cmd.Parameters.AddWithValue("appUserId", newCredential.AppUserId);
        cmd.Parameters.AddWithValue("passwordHash", newCredential.PasswordHash);
        cmd.Parameters.AddWithValue("passwordHashAlgorithm", newCredential.PasswordHashAlgorithm);
        cmd.Parameters.AddWithValue("passwordChangedAtUtc", (object?)newCredential.PasswordChangedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mustResetPassword", newCredential.MustResetPassword);
        cmd.Parameters.AddWithValue("failedLoginCount", newCredential.FailedLoginCount);
        cmd.Parameters.AddWithValue("lockedOutUntilUtc", (object?)newCredential.LockedOutUntilUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("createdDate", newCredential.CreatedDate);
        cmd.Parameters.AddWithValue("modifiedDate", newCredential.ModifiedDate);

        using var dr = cmd.ExecuteReader();
        if (dr.Read()) return MapFromReader(dr);

        throw new InvalidOperationException("AppUserCredential insert failed.");
    }

    public bool UpdateCredential(int id, AppUserCredential updatedCredential)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand(
            @"UPDATE public.appusercredential
              SET appuserid = @appUserId,
                  passwordhash = @passwordHash,
                  passwordhashalgorithm = @passwordHashAlgorithm,
                  passwordchangedatutc = @passwordChangedAtUtc,
                  mustresetpassword = @mustResetPassword,
                  failedlogincount = @failedLoginCount,
                  lockedoutuntilutc = @lockedOutUntilUtc,
                  modifieddate = @modifiedDate
              WHERE id = @id;", conn);

        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("appUserId", updatedCredential.AppUserId);
        cmd.Parameters.AddWithValue("passwordHash", updatedCredential.PasswordHash);
        cmd.Parameters.AddWithValue("passwordHashAlgorithm", updatedCredential.PasswordHashAlgorithm);
        cmd.Parameters.AddWithValue("passwordChangedAtUtc", (object?)updatedCredential.PasswordChangedAtUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("mustResetPassword", updatedCredential.MustResetPassword);
        cmd.Parameters.AddWithValue("failedLoginCount", updatedCredential.FailedLoginCount);
        cmd.Parameters.AddWithValue("lockedOutUntilUtc", (object?)updatedCredential.LockedOutUntilUtc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("modifiedDate", updatedCredential.ModifiedDate);

        return cmd.ExecuteNonQuery() > 0;
    }

    public bool DeleteCredential(int id)
    {
        using var conn = new NpgsqlConnection(_dbConfig.GetConnectionString());
        conn.Open();

        using var cmd = new NpgsqlCommand("DELETE FROM public.appusercredential WHERE id = @id;", conn);
        cmd.Parameters.AddWithValue("id", id);

        return cmd.ExecuteNonQuery() > 0;
    }

    private static AppUserCredential MapFromReader(NpgsqlDataReader dr)
    {
        return new AppUserCredential
        {
            Id = dr.GetInt32(0),
            AppUserId = dr.GetInt32(1),
            PasswordHash = dr.GetString(2),
            PasswordHashAlgorithm = dr.GetString(3),
            PasswordChangedAtUtc = dr.IsDBNull(4) ? null : dr.GetDateTime(4),
            MustResetPassword = dr.GetBoolean(5),
            FailedLoginCount = dr.GetInt32(6),
            LockedOutUntilUtc = dr.IsDBNull(7) ? null : dr.GetDateTime(7),
            CreatedDate = dr.GetDateTime(8),
            ModifiedDate = dr.GetDateTime(9)
        };
    }
}
