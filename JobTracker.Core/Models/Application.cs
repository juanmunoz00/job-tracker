namespace JobTracker.Core.Models;

public class Application
{
    public int Id { get; set; }

    public string Country { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Link { get; set; }

    public DateOnly AppliedDate { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime StatusDate { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
    public string? Compensation { get; set; }
}

public enum ApplicationStatus
{
    Applied,
    PhoneScreen,
    Interview,
    TakeHome,
    Offer,
    Rejected,
    Withdrawn
}