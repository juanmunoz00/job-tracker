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

    public async Task<ApplicationPage> GetPagedAsync(string? search, ApplicationStatus? status, int page, int pageSize)
    {
        IQueryable<Application> query = _context.Applications.AsNoTracking();

        // 1. Search: case-insensitive match on company, position or country
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escaped = search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{escaped}%";

            query = query.Where(a =>
                EF.Functions.ILike(a.Company, pattern) ||
                EF.Functions.ILike(a.Position, pattern) ||
                EF.Functions.ILike(a.Country, pattern));
        }

        // 2. Count per status (after search, before the status filter) for the chips
        var statusCounts = await query
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // 3. Status filter
        if (status is not null)
        {
            var selected = status.Value;
            query = query.Where(a => a.Status == selected);
        }

        // 4. Total rows matching search + status (for "Page X of Y")
        var totalCount = await query.CountAsync();

        // 5. Sort and take one page
        var items = await query
            .OrderByDescending(a => a.AppliedDate)
            .ThenByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ApplicationPage(items, totalCount, statusCounts);
    }

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