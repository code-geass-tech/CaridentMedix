using SixLabors.ImageSharp;

namespace CaridentMedix.Server.Models;

public class Image
{
    public DateTimeOffset CreatedAt { get; set; }

    public int Height { get; set; }

    public int Id { get; set; }

    public int Width { get; set; }

    public virtual List<Detection> Detections { get; set; } = [];

    public string Name { get; set; } = null!;

    public string OriginalImagePath { get; set; } = null!;

    public string PlottedImagePath { get; set; } = null!;
}

public class Detection
{
    public float Confidence { get; set; }

    public int ClassId { get; set; }

    public int Height { get; set; }

    public int Id { get; set; }

    public int Width { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public Rectangle Bounds => new(X, Y, Width, Height);

    public string ClassName { get; set; } = null!;
}

public class DataReport
{
    public virtual ApplicationUser User { get; set; } = null!;

    public virtual Clinic SubmittedClinic { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Id { get; set; }

    public virtual List<Image> Images { get; set; } = [];

    public string Description { get; set; } = null!;

    public string Title { get; set; }
}