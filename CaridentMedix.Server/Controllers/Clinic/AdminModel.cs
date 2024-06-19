namespace CaridentMedix.Server.Controllers.Clinic;

public class AdminModel
{
    public bool EmailConfirmed { get; set; }

    public bool IsClinicAdmin { get; set; }

    public bool PhoneNumberConfirmed { get; set; }

    public string? Email { get; set; }

    public string? ImagePath { get; set; }

    public string? Name { get; set; }

    public string? NormalizedEmail { get; set; }

    public string? PhoneNumber { get; set; }
}