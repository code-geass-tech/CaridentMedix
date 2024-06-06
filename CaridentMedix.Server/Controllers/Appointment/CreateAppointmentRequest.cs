namespace CaridentMedix.Server.Controllers.Appointment;

public class CreateAppointmentRequest
{
    public DateTimeOffset ScheduledAt { get; set; }

    public int ClinicId { get; set; }

    public int? DentistId { get; set; }
}