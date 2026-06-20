using RobotControllerApi.BoundedContexts.AppUsers.Dtos;
using RobotControllerApi.BoundedContexts.AppUsers.Models;
using RobotControllerApi.BoundedContexts.AppUsers.Persistence;
using RobotControllerApi.BoundedContexts.Shared;

namespace RobotControllerApi.BoundedContexts.AppUsers.Services;

public class AppUserService : IAppUserService
{
    private readonly IAppUserDataAccess _dataAccess;

    public AppUserService(IAppUserDataAccess dataAccess)
    {
        _dataAccess = dataAccess;
    }

    public List<AppUserResponse> GetAppUsers()
    {
        return _dataAccess.GetAppUsers().Select(MapToResponse).ToList();
    }

    public AppUserResponse? GetAppUserById(int id)
    {
        var model = _dataAccess.GetAppUserById(id);
        return model == null ? null : MapToResponse(model);
    }

    public AppUserResponse CreateAppUser(CreateAppUserRequest request)
    {
        ValidateRequest(request.Email, request.DisplayName, request.Role);
        
        if (_dataAccess.AppUserExistsByEmail(request.Email))
            throw new InvalidOperationException("A user with this email already exists.");

        var now = DateTime.UtcNow;
        var model = new AppUser
        {
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedDate = now,
            ModifiedDate = now
        };
        
        return MapToResponse(_dataAccess.CreateAppUser(model));
    }

    public bool UpdateAppUser(int id, UpdateAppUserRequest request)
    {
        ValidateRequest(request.Email, request.DisplayName, request.Role);
        
        var existing = _dataAccess.GetAppUserById(id);
        if (existing == null) return false;
        
        if (_dataAccess.AppUserExistsByEmail(request.Email, id))
            throw new InvalidOperationException("A user with this email already exists.");

        existing.Email = request.Email.Trim();
        existing.DisplayName = request.DisplayName.Trim();
        existing.Role = request.Role;
        existing.IsActive = request.IsActive;
        existing.ModifiedDate = DateTime.UtcNow;
        
        return _dataAccess.UpdateAppUser(id, existing);
    }

    public bool DeactivateAppUser(int id)
    {
        var existing = _dataAccess.GetAppUserById(id);
        if (existing == null) return false;
        
        existing.IsActive = false;
        existing.ModifiedDate = DateTime.UtcNow;
        
        return _dataAccess.UpdateAppUser(id, existing);
    }

    public bool ReactivateAppUser(int id)
    {
        var existing = _dataAccess.GetAppUserById(id);
        if (existing == null) return false;
        
        existing.IsActive = true;
        existing.ModifiedDate = DateTime.UtcNow;
        
        return _dataAccess.UpdateAppUser(id, existing);
    }

    public bool DeleteAppUser(int id)
    {
        return _dataAccess.DeleteAppUser(id);
    }

    private void ValidateRequest(string email, string displayName, string role)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");
        if (email.Length > 320) throw new ArgumentException("Email cannot exceed 320 characters.");
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName is required.");
        if (displayName.Length > 200) throw new ArgumentException("DisplayName cannot exceed 200 characters.");
        if (!DomainConstants.IsRole(role)) throw new ArgumentException("Role must be Admin or User.");
    }

    private static AppUserResponse MapToResponse(AppUser model)
    {
        return new AppUserResponse
        {
            Id = model.Id,
            Email = model.Email,
            DisplayName = model.DisplayName,
            Role = model.Role,
            IsActive = model.IsActive,
            LastLoginAtUtc = model.LastLoginAtUtc,
            CreatedDate = model.CreatedDate,
            ModifiedDate = model.ModifiedDate
        };
    }
}