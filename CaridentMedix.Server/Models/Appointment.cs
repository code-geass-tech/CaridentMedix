namespace CaridentMedix.Server.Models;

public class Appointment
{
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual Clinic Clinic { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = default!;

    public DateTimeOffset ScheduledAt { get; set; } = default!;

    public virtual Dentist? Dentist { get; set; }

    public int Id { get; set; }

    public string Status { get; set; }

    public string? ClinicCancelMessage { get; set; }

    public string? ClinicMessage { get; set; }

    public string? UserCancelMessage { get; set; }

    public string? UserMessage { get; set; } = null!;
}