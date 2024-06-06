using CaridentMedix.Server.Controllers.Clinic;

namespace CaridentMedix.Server.Controllers.Appointment;

public class AppointmentModel
{
    public ClinicModel Clinic { get; set; } = null!;

    public DateTimeOffset ScheduledAt { get; set; }

    public DentistModel? Dentist { get; set; }

    public int Id { get; set; }
}