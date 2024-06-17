using CaridentMedix.Server.Controllers.Clinic;

namespace CaridentMedix.Server.Controllers.Appointment;

public class AppointmentModel
{
    public ClinicModel Clinic { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = default!;

    public DateTimeOffset ScheduledAt { get; set; }

    public DentistModel? Dentist { get; set; }

    public int Id { get; set; }

    public string Status { get; set; }

    public string? ClinicCancelMessage { get; set; }

    public string? ClinicMessage { get; set; }

    public string? UserCancelMessage { get; set; }

    public string? UserMessage { get; set; } = null!;
}