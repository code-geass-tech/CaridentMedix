namespace CaridentMedix.Server.Models;

public class DataReport
{
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual Clinic Clinic { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = default!;

    public int Id { get; set; }

    public virtual List<Image> Images { get; set; } = [];

    public string? Description { get; set; } = null!;

    public string? Title { get; set; } = default!;
}