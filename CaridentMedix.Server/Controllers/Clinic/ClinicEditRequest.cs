using CaridentMedix.Server.Models;

namespace CaridentMedix.Server.Controllers.Clinic;

#pragma warning disable CS1591
public class ClinicEditRequest
{
    public double? Distance { get; set; }
 
    public float? Latitude { get; init; }
 
    public float? Longitude { get; init; }
 
    public List<Dentist>? Dentists { get; init; } = [];
 
    public string? Address { get; init; } = null!;
 
    public string? Description { get; init; } = null!;
 
    public string? Email { get; init; } = null!;
 
    public string? ImagePath { get; init; } = null!;
 
    public string? Name { get; init; } = null!;
 
    public string? PhoneNumber { get; init; } = null!;
 
    public string? Website { get; init; } = null!;
}