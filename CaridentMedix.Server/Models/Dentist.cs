namespace CaridentMedix.Server.Models;

public class Dentist
{
    public int Id { get; set; }

    public virtual List<Schedule> Schedules { get; set; } = [];

    public string Name { get; set; } = null!;

    public string? Email { get; set; } = null!;

    public string? PhoneNumber { get; set; } = null!;
}