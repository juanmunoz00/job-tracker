namespace JobTracker.Core.DTOs;

public class ApplicationDto
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateOnly AppliedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StatusDate { get; set; }
}