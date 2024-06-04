using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CaridentMedix.Server.Models;

/// <inheritdoc />
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Appointment> Appointments { get; set; } = null!;

    public DbSet<Clinic> Clinics { get; set; } = null!;

    public DbSet<DataReport> DataReports { get; set; } = null!;

    public DbSet<Dentist> Dentists { get; set; } = null!;

    public DbSet<Image> Images { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}