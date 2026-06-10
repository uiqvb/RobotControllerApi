using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class AppUserCredentialEF : IAppUserCredentialDataAccess
{
    private readonly RobotContext _context;

    public AppUserCredentialEF(RobotContext context)
    {
        _context = context;
    }

    public AppUserCredential? GetCredentialByAppUserId(int appUserId)
    {
        return _context.AppUserCredentials.SingleOrDefault(x => x.AppUserId == appUserId);
    }

    public AppUserCredential InsertCredential(AppUserCredential newCredential)
    {
        _context.AppUserCredentials.Add(newCredential);
        _context.SaveChanges();
        return newCredential;
    }

    public bool UpdateCredential(int id, AppUserCredential updatedCredential)
    {
        var existing = _context.AppUserCredentials.SingleOrDefault(x => x.Id == id);
        if (existing == null) return false;

        existing.AppUserId = updatedCredential.AppUserId;
        existing.PasswordHash = updatedCredential.PasswordHash;
        existing.PasswordHashAlgorithm = updatedCredential.PasswordHashAlgorithm;
        existing.PasswordChangedAtUtc = updatedCredential.PasswordChangedAtUtc;
        existing.MustResetPassword = updatedCredential.MustResetPassword;
        existing.FailedLoginCount = updatedCredential.FailedLoginCount;
        existing.LockedOutUntilUtc = updatedCredential.LockedOutUntilUtc;
        existing.ModifiedDate = updatedCredential.ModifiedDate;

        _context.SaveChanges();
        return true;
    }

    public bool DeleteCredential(int id)
    {
        var existing = _context.AppUserCredentials.SingleOrDefault(x => x.Id == id);
        if (existing == null) return false;

        _context.AppUserCredentials.Remove(existing);
        _context.SaveChanges();
        return true;
    }
}
