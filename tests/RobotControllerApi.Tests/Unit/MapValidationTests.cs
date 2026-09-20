using FluentAssertions;
using RobotControllerApi.BoundedContexts.Maps.Dtos;
using RobotControllerApi.BoundedContexts.Maps.Services;

namespace RobotControllerApi.Tests.Unit;

// Grid dimensions are the one input the robot cannot recover from being wrong: a map
// with no rows or negative columns describes a space the robot is then asked to
// navigate. MapService.ValidateRequest is private, so it is exercised through the
// public CreateMap against an in-memory persistence stand-in.
public class MapValidationTests
{
    private static MapService BuildService() => new(new InMemoryMapDataAccess());

    private static CreateMapRequest ValidRequest() => new()
    {
        Name = "Test Grid",
        Columns = 10,
        Rows = 10,
        CellSizeCm = 30,
        Description = "A valid grid.",
        IsActive = true
    };

    [Theory]
    [InlineData(0, 10, "Columns")]
    [InlineData(-5, 10, "Columns")]
    [InlineData(10, 0, "Rows")]
    [InlineData(10, -3, "Rows")]
    public void CreateMap_RejectsZeroOrNegativeDimensions(int columns, int rows, string expectedField)
    {
        var service = BuildService();

        var request = ValidRequest();
        request.Columns = columns;
        request.Rows = rows;

        var act = () => service.CreateMap(request);

        act.Should().Throw<ArgumentException>()
           .WithMessage($"{expectedField} must be greater than zero.");
    }

    [Fact]
    public void CreateMap_AcceptsAValidGrid()
    {
        var service = BuildService();

        var created = service.CreateMap(ValidRequest());

        created.Id.Should().BeGreaterThan(0);
        created.Name.Should().Be("Test Grid");
        created.Columns.Should().Be(10);
        created.Rows.Should().Be(10);
        created.CellSizeCm.Should().Be(30);
        created.IsActive.Should().BeTrue();
        created.CreatedDate.Should().NotBe(default);
    }
}
