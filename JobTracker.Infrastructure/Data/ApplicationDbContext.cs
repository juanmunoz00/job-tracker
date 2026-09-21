using JobTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Application> Applications => Set<Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Company)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Country)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Position)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Status)
                  .HasConversion<int>();   // store enum as integer

            entity.Property(e => e.StatusDate)
                  .HasDefaultValueSql("NOW()");
        });
    }
}