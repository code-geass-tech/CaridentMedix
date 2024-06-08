namespace CaridentMedix.Server.Controllers.Appointment;

public class UpdateAppointmentRequest
{
    public DateTimeOffset? ScheduledAt { get; set; }

    public string? ClinicMessage { get; set; }

    public string? Status { get; set; }

    public string? UserMessage { get; set; }
}