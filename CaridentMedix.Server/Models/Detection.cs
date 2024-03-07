using SixLabors.ImageSharp;

namespace CaridentMedix.Server.Models;

public class Detection
{
    /// <summary>
    ///     The confidence of the detection.
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    ///     The unique identifier of the class.
    /// </summary>
    public int ClassId { get; set; }

    /// <summary>
    ///     The height of the detection.
    /// </summary>
    public int Height { get; set; }

    public int Id { get; set; }

    /// <summary>
    ///     The width of the detection.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    ///     The x-coordinate of the detection.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    ///     The y-coordinate of the detection.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    ///     The bounds of the detection.
    /// </summary>
    public Rectangle Bounds => new(X, Y, Width, Height);

    /// <summary>
    ///     The class name of the detection.
    /// </summary>
    public string ClassName { get; set; } = null!;
}