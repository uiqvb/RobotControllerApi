using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace RobotControllerApi.Tests.Integration;

// End-to-end against a running container and a real PostgreSQL instance. Tagged so the
// pipeline can run them separately from the unit suite:
//   dotnet test --filter "Category!=Integration"
//   dotnet test --filter "Category=Integration"
[Collection("api")]
[Trait("Category", "Integration")]
public class MapsApiTests
{
    private readonly ApiFixture _api;

    public MapsApiTests(ApiFixture api) => _api = api;

    private static object ValidMap(string name) => new
    {
        name,
        columns = 6,
        rows = 6,
        cellSizeCm = 20,
        description = "created by the integration suite",
        isActive = true
    };

    private static string UniqueName() => $"itest-map-{Guid.NewGuid():N}";

    private async Task<(int Id, string Name)> CreateMapAsync()
    {
        var name = UniqueName();
        var response = await _api.Authenticated.PostAsJsonAsync("/api/maps", ValidMap(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("id").GetInt32(), name);
    }

    [Fact]
    public async Task Health_ReportsTheDatabaseIsConnected()
    {
        // Anonymous on purpose: Docker's HEALTHCHECK has no credentials.
        var response = await _api.Anonymous.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("\"database\":\"connected\"");
    }

    [Fact]
    public async Task CreateMap_WithAValidBody_Returns201AndAnId()
    {
        var response = await _api.Authenticated.PostAsJsonAsync("/api/maps", ValidMap(UniqueName()));

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMapById_ReturnsTheMapThatWasCreated()
    {
        var (id, name) = await CreateMapAsync();

        var response = await _api.Authenticated.GetAsync($"/api/maps/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetInt32().Should().Be(id);
        body.GetProperty("name").GetString().Should().Be(name);
    }

    [Fact]
    public async Task UpdateMap_ChangesTheNameAndASubsequentGetConfirmsIt()
    {
        var (id, _) = await CreateMapAsync();
        var newName = UniqueName();

        var update = await _api.Authenticated.PutAsJsonAsync($"/api/maps/{id}", ValidMap(newName));

        // The controller answers 204 NoContent. Accepting either success code keeps the
        // test about the outcome rather than the choice between two correct answers -
        // the assertion that carries the weight is the read-back below.
        update.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var readBack = await _api.Authenticated.GetAsync($"/api/maps/{id}");
        readBack.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await readBack.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be(newName);
    }

    [Fact]
    public async Task DeleteMap_RemovesIt_AndTheSubsequentGetReturns404()
    {
        var (id, _) = await CreateMapAsync();

        var delete = await _api.Authenticated.DeleteAsync($"/api/maps/{id}");
        delete.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);

        var readBack = await _api.Authenticated.GetAsync($"/api/maps/{id}");
        readBack.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateMap_WithNegativeColumns_Returns400()
    {
        var response = await _api.Authenticated.PostAsJsonAsync("/api/maps", new
        {
            name = UniqueName(),
            columns = -5,
            rows = 6,
            cellSizeCm = 20,
            description = "invalid dimensions",
            isActive = true
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMaps_WithoutAnAuthorizationHeader_Returns401()
    {
        var response = await _api.Anonymous.GetAsync("/api/maps");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
