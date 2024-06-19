using CaridentMedix.Server.Controllers.Clinic;

namespace CaridentMedix.Server.Controllers.Appointment;

public class AppointmentModel
{
    public AppointmentUserModel User { get; set; } = null!;

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

public class AppointmentUserModel
{
    public string? Email { get; set; } = null!;

    public string? ImagePath { get; set; }

    public string? Name { get; set; }

    public string? PhoneNumber { get; set; }
}