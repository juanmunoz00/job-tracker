using JobTracker.Core.Models;

namespace JobTracker.Core.Interfaces;

public interface IApplicationRepository
{
    Task<ApplicationPage> GetPagedAsync(string? search, ApplicationStatus? status, int page, int pageSize);
    Task<IEnumerable<Application>> GetAllAsync();
    Task<Application?> GetByIdAsync(int id);
    Task<Application> CreateAsync(Application application);
    Task<Application> UpdateAsync(Application application);
    Task<Application> PatchStatusAsync(int id, ApplicationStatus status);
    Task DeleteAsync(int id);
}