namespace CaridentMedix.Server.Models;

public class Clinic
{
    public float Latitude { get; set; }

    public float Longitude { get; set; }

    public int Id { get; set; }

    public virtual List<DataReport> DataReports { get; set; } = [];

    public virtual List<Dentist> Dentists { get; set; } = [];

    public string Address { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; } = null!;

    public string? Email { get; set; } = null!;

    public string? ImagePath { get; set; } = null!;

    public string? PhoneNumber { get; set; } = null!;

    public string? Website { get; set; } = null!;
}