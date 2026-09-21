using FluentAssertions;
using JobTracker.Core.Models;

namespace JobTracker.Tests.Domain;

public class ApplicationEntityTests
{
    [Fact]
    public void NewApplication_DefaultStatus_ShouldBeApplied()
    {
        var app = new Application();

        app.Status.Should().Be(ApplicationStatus.Applied);
    }

    [Fact]
    public void NewApplication_StatusDate_ShouldDefaultToUtcNow()
    {
        var before = DateTime.UtcNow;
        var app = new Application();
        var after = DateTime.UtcNow;

        app.StatusDate.Should().BeOnOrAfter(before)
                               .And.BeOnOrBefore(after);
    }

    [Fact]
    public void NewApplication_StringFields_ShouldDefaultToEmpty()
    {
        var app = new Application();

        app.Country.Should().BeEmpty();
        app.Company.Should().BeEmpty();
        app.Position.Should().BeEmpty();
    }

    [Fact]
    public void NewApplication_NullableFields_ShouldBeNull()
    {
        var app = new Application();

        app.Description.Should().BeNull();
        app.Link.Should().BeNull();
        app.Notes.Should().BeNull();
        app.Compensation.Should().BeNull();
    }

    [Fact]
    public void Application_CanSetAllFields()
    {
        var appliedDate = new DateOnly(2026, 9, 21);

        var app = new Application
        {
            Company     = "Acme Corp",
            Country     = "US",
            Position    = "Senior Software Engineer",
            Description = "Full-stack role",
            Link        = "https://acme.com/jobs/123",
            AppliedDate = appliedDate,
            Status      = ApplicationStatus.Interview,
            Notes       = "Referral from LinkedIn",
            Compensation = "$150k"
        };

        app.Company.Should().Be("Acme Corp");
        app.Country.Should().Be("US");
        app.Position.Should().Be("Senior Software Engineer");
        app.AppliedDate.Should().Be(appliedDate);
        app.Status.Should().Be(ApplicationStatus.Interview);
        app.Compensation.Should().Be("$150k");
    }
}