namespace CaridentMedix.Server.Controllers.Clinic;

public class ClinicModel
{
    public double Distance { get; set; }

    public float Latitude { get; init; }

    public float Longitude { get; init; }

    public int Id { get; init; }

    public List<DentistModel> Dentists { get; init; } = [];

    public string Address { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string? Description { get; init; } = null!;

    public string? Email { get; init; } = null!;

    public string? ImagePath { get; init; } = null!;

    public string? PhoneNumber { get; init; } = null!;

    public string? Website { get; init; } = null!;
}