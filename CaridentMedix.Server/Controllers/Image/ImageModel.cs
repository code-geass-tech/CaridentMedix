using CaridentMedix.Server.Models;

namespace CaridentMedix.Server.Controllers.Image;

public class ImageModel
{
    public static string ImageBaseUrl = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public int Height { get; set; }

    public int Id { get; set; }

    public int Width { get; set; }

    public virtual List<Detection> Detections { get; set; } = [];

    public string Name { get; set; } = null!;

    public string OriginalImagePath { get; set; } = null!;

    public string PlottedImagePath { get; set; } = null!;
}