namespace JobTracker.Core.Models;

public record ApplicationPage(
    IReadOnlyList<Application> Items,
    int TotalCount,
    IReadOnlyDictionary<ApplicationStatus, int> StatusCounts);