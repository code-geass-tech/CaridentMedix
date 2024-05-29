using CaridentMedix.Server.Controllers.Image;

namespace CaridentMedix.Server.Controllers.Admin;

public class UserModel
{
    public bool EmailConfirmed { get; set; }

    public bool IsClinicAdmin { get; set; }

    public bool IsDeleted { get; set; }

    public bool LockoutEnabled { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public ICollection<DataReportResponse> DataReports { get; set; }

    public ICollection<ImageResponse> Images { get; set; }

    public int AccessFailedCount { get; set; }

    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? NormalizedEmail { get; set; }

    public string? PhoneNumber { get; set; }
}