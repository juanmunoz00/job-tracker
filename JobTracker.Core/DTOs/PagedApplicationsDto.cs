namespace JobTracker.Core.DTOs;

public class PagedApplicationsDto
{
    public IEnumerable<ApplicationDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public int AllCount { get; set; }
    public Dictionary<string, int> StatusCounts { get; set; } = [];
}