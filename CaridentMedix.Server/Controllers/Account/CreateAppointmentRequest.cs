using CaridentMedix.Server.Controllers.Clinic;

namespace CaridentMedix.Server.Controllers.Account;

public class CreateAppointmentRequest
{
    public DateTimeOffset ScheduledAt { get; set; }

    public int ClinicId { get; set; }

    public int? DentistId { get; set; }
}

public class AppointmentModel
{
    public int Id { get; set; }

    public DateTimeOffset ScheduledAt { get; set; }

    public ClinicModel Clinic { get; set; } = null!;

    public DentistModel? Dentist { get; set; }

}