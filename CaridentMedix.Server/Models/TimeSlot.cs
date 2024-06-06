namespace CaridentMedix.Server.Models;

public class TimeSlot
{
    public virtual Clinic Clinic { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public int Id { get; set; }

    public List<Appointment> Appointments { get; set; } = [];

    public TimeOnly EndTime { get; set; }

    public TimeOnly StartTime { get; set; }
}