namespace CaridentMedix.Server.Controllers.Image;

public class DataReportModel
{
    public DateTimeOffset CreatedAt { get; set; }

    public int Id { get; set; }

    public virtual List<ImageModel> Images { get; set; } = [];

    public string Description { get; set; } = null!;

    public string Title { get; set; } = null!;
}