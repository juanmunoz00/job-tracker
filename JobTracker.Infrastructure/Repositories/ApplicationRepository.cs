using JobTracker.Core.Interfaces;
using JobTracker.Core.Models;
using JobTracker.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Application>> GetAllAsync()
        => await _context.Applications
                         .AsNoTracking()
                         .OrderByDescending(a => a.AppliedDate)
                         .ToListAsync();

    public async Task<Application?> GetByIdAsync(int id)
        => await _context.Applications
                         .AsNoTracking()
                         .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Application> CreateAsync(Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> UpdateAsync(Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Application> PatchStatusAsync(int id, ApplicationStatus status)
    {
        var application = await _context.Applications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Application {id} not found.");

        application.Status = status;
        application.StatusDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task DeleteAsync(int id)
    {
        var application = await _context.Applications.FindAsync(id)
            ?? throw new KeyNotFoundException($"Application {id} not found.");

        _context.Applications.Remove(application);
        await _context.SaveChangesAsync();
    }
}