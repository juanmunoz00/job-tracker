namespace JobTracker.Core.DTOs;

public class CreateApplicationDto
{
    public string Company { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateOnly AppliedDate { get; set; }
}