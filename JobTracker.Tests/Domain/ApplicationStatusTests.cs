using FluentAssertions;
using JobTracker.Core.Models;

namespace JobTracker.Tests.Domain;

public class ApplicationStatusTests
{
    [Fact]
    public void ApplicationStatus_ShouldDefineAllExpectedValues()
    {
        var defined = Enum.GetNames<ApplicationStatus>();

        defined.Should().Contain([
            "Applied",
            "PhoneScreen",
            "Interview",
            "TakeHome",
            "Offer",
            "Rejected",
            "Withdrawn"
        ]);
    }

    [Theory]
    [InlineData(ApplicationStatus.Applied,     0)]
    [InlineData(ApplicationStatus.PhoneScreen, 1)]
    [InlineData(ApplicationStatus.Interview,   2)]
    [InlineData(ApplicationStatus.TakeHome,    3)]
    [InlineData(ApplicationStatus.Offer,       4)]
    [InlineData(ApplicationStatus.Rejected,    5)]
    [InlineData(ApplicationStatus.Withdrawn,   6)]
    public void ApplicationStatus_IntValues_ShouldBeStable(
        ApplicationStatus status, int expectedValue)
    {
        // Stable int values matter — EF Core stores these in the DB
        ((int)status).Should().Be(expectedValue);
    }
}