using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;

namespace RobotControllerApi.Infrastructure.DataAccess.EFCore;

public class AppUserEF : IAppUserDataAccess
{
    private readonly RobotContext _context;

    public AppUserEF(RobotContext context)
    {
        _context = context;
    }

    public List<AppUser> GetAppUsers()
    {
        return _context.AppUsers.OrderBy(x => x.Id).ToList();
    }

    public AppUser? GetAppUserById(int id)
    {
        return _context.AppUsers.Find(id);
    }

    public AppUser? GetAppUserByEmail(string email)
    {
        return _context.AppUsers.FirstOrDefault(x => x.Email.ToUpper() == email.ToUpper());
    }

    public bool AppUserExistsByEmail(string email, int? excludeId = null)
    {
        var query = _context.AppUsers.Where(x => x.Email.ToUpper() == email.ToUpper());
        
        if (excludeId.HasValue)
        {
            query = query.Where(x => x.Id != excludeId.Value);
        }

        return query.Any();
    }

    public AppUser CreateAppUser(AppUser newAppUser)
    {
        _context.AppUsers.Add(newAppUser);
        _context.SaveChanges();
        return newAppUser;
    }

    public bool UpdateAppUser(int id, AppUser updatedAppUser)
    {
        var existing = _context.AppUsers.Find(id);
        if (existing == null) return false;

        existing.Email = updatedAppUser.Email;
        existing.DisplayName = updatedAppUser.DisplayName;
        existing.Role = updatedAppUser.Role;
        existing.IsActive = updatedAppUser.IsActive;
        existing.LastLoginAtUtc = updatedAppUser.LastLoginAtUtc;
        existing.CreatedDate = updatedAppUser.CreatedDate;
        existing.ModifiedDate = updatedAppUser.ModifiedDate;

        return _context.SaveChanges() > 0;
    }

    public bool DeleteAppUser(int id)
    {
        var existing = _context.AppUsers.Find(id);
        if (existing == null) return false;

        _context.AppUsers.Remove(existing);
        return _context.SaveChanges() > 0;
    }
}