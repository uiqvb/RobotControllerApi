namespace RobotControllerApi.Infrastructure.DataAccess;

public class DbConfig
{
    private readonly IConfiguration _configuration;

    public DbConfig(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Resolution order, most specific first:
    //
    //   1. ConnectionStrings:DefaultConnection - what User Secrets supplies on a dev machine,
    //      and what ConnectionStrings__DefaultConnection binds to as a single env var.
    //   2. DB_HOST / DB_PORT / DB_NAME / DB_USER / DB_PASSWORD - discrete parts, which is what
    //      a container gets. Composing them here means one image can be pointed at the staging
    //      database or the production one purely by changing its environment, with nothing
    //      about either baked into the build.
    //
    // The parts carry defaults so a bare `dotnet run` still reaches a local PostgreSQL, but
    // DB_PASSWORD deliberately has none: a default password would be a credential living in
    // source, and every environment that needs one is expected to pass it in.
    public string GetConnectionString()
    {
        var configured = _configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var host = Read("DB_HOST", "localhost");
        var port = Read("DB_PORT", "5432");
        var database = Read("DB_NAME", "robotcontroller");
        var username = Read("DB_USER", "postgres");
        var password = Read("DB_PASSWORD", string.Empty);

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }

    public string GetPersistenceProvider()
    {
        return _configuration["Persistence:Provider"] ?? "ADO";
    }

    private string Read(string key, string fallback)
    {
        var value = _configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
