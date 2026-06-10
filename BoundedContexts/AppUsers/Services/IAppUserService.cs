using RobotControllerApi.BoundedContexts.AppUsers.Dtos;

namespace RobotControllerApi.BoundedContexts.AppUsers.Services;

public interface IAppUserService
{
    List<AppUserResponse> GetAppUsers();
    AppUserResponse? GetAppUserById(int id);
    AppUserResponse CreateAppUser(CreateAppUserRequest request);
    bool UpdateAppUser(int id, UpdateAppUserRequest request);
    bool DeactivateAppUser(int id);
    bool ReactivateAppUser(int id);
    bool DeleteAppUser(int id);
}