using CaridentMedix.Server.Controllers.Clinic;

namespace CaridentMedix.Server.Controllers.Admin;

public class CreateClinicResponse : BaseResponse
{
    public ClinicModel Clinic { get; set; } = null!;
}