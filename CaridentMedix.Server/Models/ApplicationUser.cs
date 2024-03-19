using Microsoft.AspNetCore.Identity;

namespace CaridentMedix.Server.Models;

public class ApplicationUser : IdentityUser
{
    public virtual Dentist? Dentist { get; set; }

    public virtual ICollection<Clinic> Clinics { get; set; } = [];

    public virtual ICollection<DataReport> DataReports { get; set; } = [];

    public virtual ICollection<Image> Images { get; set; } = [];

    public string? ImagePath { get; set; } = null!;
}