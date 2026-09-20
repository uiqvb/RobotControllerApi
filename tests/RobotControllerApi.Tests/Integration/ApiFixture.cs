using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace RobotControllerApi.Tests.Integration;

// Shared setup for the integration suite: where the API is, and a credential to talk
// to it with.
//
// The base URL comes from API_BASE_URL so the same compiled tests run against the
// test stack published on the host (http://localhost:8092) and against the service
// name on the compose network when the pipeline runs them in a container
// (http://robot-test-app:8080).
public sealed class ApiFixture : IAsyncLifetime
{
    public const string DefaultBaseUrl = "http://localhost:8092";

    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("API_BASE_URL") is { Length: > 0 } fromEnv
            ? fromEnv.TrimEnd('/')
            : DefaultBaseUrl;

    // Unique per run, so a re-run against a database that was not reset does not
    // collide with the account the previous run created.
    private readonly string _email = $"itest-{Guid.NewGuid():N}@test.local";
    private const string Password = "IntegrationPass123!";

    public HttpClient Authenticated { get; private set; } = null!;
    public HttpClient Anonymous { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Anonymous = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };

        // /api/auth/register is anonymous, so the suite can provision its own caller
        // rather than depending on a seeded account.
        var response = await Anonymous.PostAsJsonAsync("/api/auth/register", new
        {
            email = _email,
            displayName = "Integration Test User",
            password = Password
        });

        response.EnsureSuccessStatusCode();

        Authenticated = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        Authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_email}:{Password}")));
    }

    public Task DisposeAsync()
    {
        Authenticated?.Dispose();
        Anonymous?.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>;
