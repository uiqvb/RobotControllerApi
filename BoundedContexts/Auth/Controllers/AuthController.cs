using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Auth.Dtos;
using RobotControllerApi.BoundedContexts.Auth.Services;

namespace RobotControllerApi.BoundedContexts.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly CurrentUserAccessor _currentUserAccessor;

    public AuthController(AuthService authService, CurrentUserAccessor currentUserAccessor)
    {
        _authService = authService;
        _currentUserAccessor = currentUserAccessor;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public ActionResult Register(RegisterRequest request)
    {
        try
        {
            return Ok(_authService.Register(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public ActionResult Login(LoginRequest request)
    {
        try
        {
            return Ok(_authService.ValidateLogin(request.Email, request.Password));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpGet("me")]
    public ActionResult Me()
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            return Ok(_authService.GetCurrentUser(appUserId));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Basic)]
    [HttpPost("verify-password")]
    public ActionResult VerifyPassword(VerifyPasswordRequest request)
    {
        try
        {
            var appUserId = _currentUserAccessor.GetRequiredAppUserId(User);
            return Ok(new VerifyPasswordResponse
            {
                IsValid = _authService.VerifyPassword(appUserId, request.Password)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
