namespace CaridentMedix.Server.Models;

public class Dentist
{
    public int Id { get; set; }

    public string? Email { get; set; } = null!;


    public string Name { get; set; } = null!;

    public string? PhoneNumber { get; set; } = null!;
}