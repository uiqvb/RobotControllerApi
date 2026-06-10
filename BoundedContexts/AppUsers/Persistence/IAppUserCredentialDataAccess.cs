using RobotControllerApi.BoundedContexts.AppUsers.Models;

namespace RobotControllerApi.BoundedContexts.AppUsers.Persistence;

public interface IAppUserCredentialDataAccess
{
    AppUserCredential? GetCredentialByAppUserId(int appUserId);
    AppUserCredential InsertCredential(AppUserCredential newCredential);
    bool UpdateCredential(int id, AppUserCredential updatedCredential);
    bool DeleteCredential(int id);
}