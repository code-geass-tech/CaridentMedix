using Microsoft.AspNetCore.Identity;

namespace CaridentMedix.Server.Models;

public class ApplicationUser : IdentityUser
{
    public bool IsClinicAdmin { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Clinic? Clinic { get; set; }

    public virtual ICollection<DataReport> DataReports { get; set; } = [];

    public virtual ICollection<Image> Images { get; set; } = [];

    public string? ImagePath { get; set; } = null!;
}