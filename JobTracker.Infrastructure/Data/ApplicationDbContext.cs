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
    public DbSet<User> Users => Set<User>();

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
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.HasIndex(e => e.Username)
                  .IsUnique();

            entity.Property(e => e.PasswordHash)
                  .IsRequired();

            entity.Property(e => e.Role)
                  .HasConversion<string>()
                  .HasMaxLength(20);
        });        
    }
}