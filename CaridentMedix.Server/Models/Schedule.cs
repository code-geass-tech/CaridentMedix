namespace CaridentMedix.Server.Models;

public class Schedule
{
    public int Id { get; set; }

    public string Day { get; set; } = null!;

    public string Time { get; set; } = null!;
}