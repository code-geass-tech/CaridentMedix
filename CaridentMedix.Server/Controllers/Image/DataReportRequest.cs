namespace CaridentMedix.Server.Controllers.Image;

public class DataReportRequest
{
    public ICollection<string> ImageIds { get; set; }

    public string? Description { get; set; }

    public string? Title { get; set; }
}