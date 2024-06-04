using CaridentMedix.Server.Controllers.Clinic;

namespace CaridentMedix.Server.Controllers.Image;

public class SharedDataReportRequest
{
    public int DataReportId { get; set; }

    public int? ClinicId { get; set; }

    public string? ClinicName { get; set; }
}
