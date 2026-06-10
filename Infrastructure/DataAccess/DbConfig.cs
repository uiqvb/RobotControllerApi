namespace RobotControllerApi.Infrastructure.DataAccess;

public class DbConfig
{
    private readonly IConfiguration _configuration;

    public DbConfig(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        return _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public string GetPersistenceProvider()
    {
        return _configuration["Persistence:Provider"] ?? "ADO";
    }
}