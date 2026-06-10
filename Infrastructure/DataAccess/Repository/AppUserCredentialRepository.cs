using Npgsql;
using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class AppUserCredentialRepository : IAppUserCredentialDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public AppUserCredentialRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public AppUserCredential? GetCredentialByAppUserId(int appUserId)
    {
        return _repo.ExecuteReader<AppUserCredential>(
            ConnectionString,
            @"SELECT id, appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate
              FROM public.appusercredential
              WHERE appuserid = @appuserid;",
            new NpgsqlParameter[] { new("appuserid", appUserId) })
            .SingleOrDefault();
    }

    public AppUserCredential InsertCredential(AppUserCredential newCredential)
    {
        return _repo.ExecuteReader<AppUserCredential>(
            ConnectionString,
            @"INSERT INTO public.appusercredential
              (appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate)
              VALUES (@AppUserId, @PasswordHash, @PasswordHashAlgorithm, @PasswordChangedAtUtc, @MustResetPassword, @FailedLoginCount, @LockedOutUntilUtc, @CreatedDate, @ModifiedDate)
              RETURNING id, appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate;",
            new NpgsqlParameter[]
            {
                new("AppUserId", newCredential.AppUserId),
                new("PasswordHash", newCredential.PasswordHash),
                new("PasswordHashAlgorithm", newCredential.PasswordHashAlgorithm),
                new("PasswordChangedAtUtc", (object?)newCredential.PasswordChangedAtUtc ?? DBNull.Value),
                new("MustResetPassword", newCredential.MustResetPassword),
                new("FailedLoginCount", newCredential.FailedLoginCount),
                new("LockedOutUntilUtc", (object?)newCredential.LockedOutUntilUtc ?? DBNull.Value),
                new("CreatedDate", newCredential.CreatedDate),
                new("ModifiedDate", newCredential.ModifiedDate),
            })
            .Single();
    }

    public bool UpdateCredential(int id, AppUserCredential updatedCredential)
    {
        var result = _repo.ExecuteReader<AppUserCredential>(
            ConnectionString,
            @"UPDATE public.appusercredential
              SET appuserid = @AppUserId,
                  passwordhash = @PasswordHash,
                  passwordhashalgorithm = @PasswordHashAlgorithm,
                  passwordchangedatutc = @PasswordChangedAtUtc,
                  mustresetpassword = @MustResetPassword,
                  failedlogincount = @FailedLoginCount,
                  lockedoutuntilutc = @LockedOutUntilUtc,
                  modifieddate = @ModifiedDate
              WHERE id = @Id
              RETURNING id, appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate;",
            new NpgsqlParameter[]
            {
                new("Id", id),
                new("AppUserId", updatedCredential.AppUserId),
                new("PasswordHash", updatedCredential.PasswordHash),
                new("PasswordHashAlgorithm", updatedCredential.PasswordHashAlgorithm),
                new("PasswordChangedAtUtc", (object?)updatedCredential.PasswordChangedAtUtc ?? DBNull.Value),
                new("MustResetPassword", updatedCredential.MustResetPassword),
                new("FailedLoginCount", updatedCredential.FailedLoginCount),
                new("LockedOutUntilUtc", (object?)updatedCredential.LockedOutUntilUtc ?? DBNull.Value),
                new("ModifiedDate", updatedCredential.ModifiedDate),
            });

        return result.Any();
    }

    public bool DeleteCredential(int id)
    {
        var result = _repo.ExecuteReader<AppUserCredential>(
            ConnectionString,
            @"DELETE FROM public.appusercredential
              WHERE id = @id
              RETURNING id, appuserid, passwordhash, passwordhashalgorithm, passwordchangedatutc, mustresetpassword, failedlogincount, lockedoutuntilutc, createddate, modifieddate;",
            new NpgsqlParameter[] { new("id", id) });

        return result.Any();
    }
}
