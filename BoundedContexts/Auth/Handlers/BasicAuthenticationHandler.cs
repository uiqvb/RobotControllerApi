using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using RobotControllerApi.BoundedContexts.Auth.Constants;
using RobotControllerApi.BoundedContexts.Auth.Services;

namespace RobotControllerApi.BoundedContexts.Auth.Handlers;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AuthService _authService;

    public BasicAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AuthService authService)
        : base(options, logger, encoder)
    {
        _authService = authService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string email;
        string password;

        try
        {
            var authHeader = AuthenticationHeaderValue.Parse(authHeaderValues.ToString());

            if (!"Basic".Equals(authHeader.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return FailAuthentication();
            }

            if (string.IsNullOrWhiteSpace(authHeader.Parameter))
            {
                return FailAuthentication();
            }

            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var decodedCredentials = Encoding.UTF8.GetString(credentialBytes);
            var credentials = decodedCredentials.Split(':', 2);

            if (credentials.Length != 2 || string.IsNullOrWhiteSpace(credentials[0]) || string.IsNullOrWhiteSpace(credentials[1]))
            {
                return FailAuthentication();
            }

            email = credentials[0].Trim();
            password = credentials[1];
        }
        catch
        {
            return FailAuthentication();
        }

        try
        {
            var user = _authService.ValidateBasicAuthentication(email, password);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(AuthClaimTypes.AppUserId, user.Id.ToString()),
                new(AuthClaimTypes.ActorType, AuthClaimTypes.HumanUserActor),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.DisplayName),
                new("name", user.DisplayName)
            };

            if (!string.IsNullOrWhiteSpace(user.Role))
            {
                claims.Add(new Claim(ClaimTypes.Role, user.Role.Trim()));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Logger.LogDebug(ex, "Basic authentication failed for {Email}.", email);
            return FailAuthentication();
        }
    }


    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;

        if (!IsAjaxOrApiRequest())
        {
            Response.Headers["WWW-Authenticate"] = @"Basic realm=""RobotControllerApi""";
        }

        return Task.CompletedTask;
    }

    private bool IsAjaxOrApiRequest()
    {
        if (Request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
            "XMLHttpRequest".Equals(requestedWith.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }

    private Task<AuthenticateResult> FailAuthentication()
    {
        return Task.FromResult(AuthenticateResult.Fail("Basic authentication failed."));
    }
}
