namespace CaridentMedix.Server.Controllers.Image;

public class CreateSharedDataReportRequest
{
    public int ClinicId { get; set; }

    public int[] ImageIds { get; set; }

    public string? Description { get; set; }

    public string? Title { get; set; }
}