using Npgsql;
using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.Repository;

public class AppUserRepository : IAppUserDataAccess, IRepository
{
    private readonly DbConfig _dbConfig;
    private IRepository _repo => this;

    public AppUserRepository(DbConfig dbConfig)
    {
        _dbConfig = dbConfig;
    }

    private string ConnectionString => _dbConfig.GetConnectionString();

    public List<AppUser> GetAppUsers()
    {
        return _repo.ExecuteReader<AppUser>(
            ConnectionString,
            @"SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate
              FROM public.appuser
              ORDER BY id;");
    }

    public AppUser? GetAppUserById(int id)
    {
        return _repo.ExecuteReader<AppUser>(
            ConnectionString,
            @"SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate
              FROM public.appuser
              WHERE id = @id;",
            new NpgsqlParameter[] { new("id", id) })
            .SingleOrDefault();
    }

    public AppUser? GetAppUserByEmail(string email)
    {
        return _repo.ExecuteReader<AppUser>(
            ConnectionString,
            @"SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate
              FROM public.appuser
              WHERE UPPER(email) = UPPER(@email);",
            new NpgsqlParameter[] { new("email", email) })
            .SingleOrDefault();
    }

    public bool AppUserExistsByEmail(string email, int? excludeId = null)
    {
        var sql = excludeId.HasValue
            ? @"SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate 
                FROM public.appuser 
                WHERE UPPER(email) = UPPER(@email) AND id <> @excludeId LIMIT 1;"
            : @"SELECT id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate 
                FROM public.appuser 
                WHERE UPPER(email) = UPPER(@email) LIMIT 1;";

        var parameters = new List<NpgsqlParameter> { new("email", email) };
        if (excludeId.HasValue) parameters.Add(new("excludeId", excludeId.Value));

        var result = _repo.ExecuteReader<AppUser>(ConnectionString, sql, parameters.ToArray());
        return result.Any();
    }

    public AppUser CreateAppUser(AppUser newAppUser)
    {
        return _repo.ExecuteReader<AppUser>(
            ConnectionString,
            @"INSERT INTO public.appuser
              (email, displayname, role, isactive, lastloginatutc, createddate, modifieddate)
              VALUES (@Email, @DisplayName, @Role, @IsActive, @LastLoginAtUtc, @CreatedDate, @ModifiedDate)
              RETURNING id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate;",
            new NpgsqlParameter[]
            {
                new NpgsqlParameter("@Email", (object?)newAppUser.Email ?? DBNull.Value),
                new NpgsqlParameter("@DisplayName", (object?)newAppUser.DisplayName ?? DBNull.Value),
                new NpgsqlParameter("@Role", (object?)newAppUser.Role ?? DBNull.Value),
                new NpgsqlParameter("@IsActive", (object?)newAppUser.IsActive ?? DBNull.Value),
                new NpgsqlParameter("@LastLoginAtUtc", (object?)newAppUser.LastLoginAtUtc ?? DBNull.Value),
                new NpgsqlParameter("@CreatedDate", (object?)newAppUser.CreatedDate ?? DBNull.Value),
                new NpgsqlParameter("@ModifiedDate", (object?)newAppUser.ModifiedDate ?? DBNull.Value),
            }).Single();
    }

    public bool UpdateAppUser(int id, AppUser updatedAppUser)
    {
        var result = _repo.ExecuteReader<AppUser>(
            ConnectionString,
            @"UPDATE public.appuser
              SET email = @Email, displayname = @DisplayName, role = @Role, isactive = @IsActive, lastloginatutc = @LastLoginAtUtc, createddate = @CreatedDate, modifieddate = @ModifiedDate
              WHERE id = @id
              RETURNING id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate;",
            new NpgsqlParameter[]
            {
                new("id", id),
                new NpgsqlParameter("@Email", (object?)updatedAppUser.Email ?? DBNull.Value),
                new NpgsqlParameter("@DisplayName", (object?)updatedAppUser.DisplayName ?? DBNull.Value),
                new NpgsqlParameter("@Role", (object?)updatedAppUser.Role ?? DBNull.Value),
                new NpgsqlParameter("@IsActive", (object?)updatedAppUser.IsActive ?? DBNull.Value),
                new NpgsqlParameter("@LastLoginAtUtc", (object?)updatedAppUser.LastLoginAtUtc ?? DBNull.Value),
                new NpgsqlParameter("@CreatedDate", (object?)updatedAppUser.CreatedDate ?? DBNull.Value),
                new NpgsqlParameter("@ModifiedDate", (object?)updatedAppUser.ModifiedDate ?? DBNull.Value),
            });

        return result.Any();
    }

    public bool DeleteAppUser(int id)
    {
        var result = _repo.ExecuteReader<AppUser>(
            ConnectionString,
            @"DELETE FROM public.appuser
              WHERE id = @id
              RETURNING id, email, displayname, role, isactive, lastloginatutc, createddate, modifieddate;",
            new NpgsqlParameter[] { new("id", id) });

        return result.Any();
    }
}