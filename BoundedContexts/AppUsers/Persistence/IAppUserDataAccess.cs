using RobotControllerApi.BoundedContexts.AppUsers.Models;

namespace RobotControllerApi.BoundedContexts.AppUsers.Persistence;

public interface IAppUserDataAccess
{
    List<AppUser> GetAppUsers();
    AppUser? GetAppUserById(int id);
    AppUser? GetAppUserByEmail(string email);
    bool AppUserExistsByEmail(string email, int? excludeId = null);
    AppUser CreateAppUser(AppUser newAppUser);
    bool UpdateAppUser(int id, AppUser updatedAppUser);
    bool DeleteAppUser(int id);
}