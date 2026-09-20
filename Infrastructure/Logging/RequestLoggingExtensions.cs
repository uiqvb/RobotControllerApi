using System.Text;

namespace RobotControllerApi.Infrastructure.Logging;

/// <summary>
/// The two lines Program.cs needs to switch console request logging on.
/// </summary>
public static class RequestLoggingExtensions
{
    public const string ConfigurationSection = "RequestLogging";

    public static IServiceCollection AddSpectreRequestLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RequestLoggingOptions>(configuration.GetSection(ConfigurationSection));
        return services;
    }

    /// <summary>
    /// Register after UseStaticFiles (so the dashboard's own assets stay out of the console)
    /// and before UseAuthentication (so 401s and 403s are still reported).
    /// </summary>
    public static IApplicationBuilder UseSpectreRequestLogging(this IApplicationBuilder app)
    {
        // The panel borders are Unicode box-drawing characters. A Windows console left on
        // code page 1252 cannot represent them and prints '?' instead; 437 happens to have
        // the glyphs, so whether the output looks right is pure luck without this.
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // No real console attached (service host, redirected output). Not worth failing over.
        }

        return app.UseMiddleware<SpectreRequestLoggingMiddleware>();
    }
}
