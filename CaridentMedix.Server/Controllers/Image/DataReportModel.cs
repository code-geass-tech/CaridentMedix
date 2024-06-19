namespace CaridentMedix.Server.Controllers.Image;

public class DataReportModel
{
    public DataReportUserModel User { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public int Id { get; set; }

    public virtual List<ImageModel> Images { get; set; } = [];

    public string Description { get; set; } = null!;

    public string Title { get; set; } = null!;
}

public class DataReportUserModel
{
    public string Email { get; set; } = null!;

    public string? ImagePath { get; set; }

    public string? Name { get; set; }

    public string? PhoneNumber { get; set; }
}