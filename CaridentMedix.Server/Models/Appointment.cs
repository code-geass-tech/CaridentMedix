namespace CaridentMedix.Server.Models;

public class Appointment
{
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual Clinic Clinic { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = default!;

    public DateTimeOffset ScheduledAt { get; set; } = default!;

    public virtual Dentist? Dentist { get; set; }

    public int Id { get; set; }
}