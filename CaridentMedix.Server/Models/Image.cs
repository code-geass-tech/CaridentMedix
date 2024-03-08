namespace CaridentMedix.Server.Models;

/// <summary>
///     Represents an image uploaded by the user.
/// </summary>
public class Image
{
    /// <summary>
    ///     The user who uploaded the image.
    /// </summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>
    ///     The date and time the image was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///     The height of the image.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    ///     The unique identifier of the image.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     The width of the image.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    ///     A list of detections found in the image.
    /// </summary>
    public virtual List<Detection> Detections { get; set; } = [];

    /// <summary>
    ///     The name of the image.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    ///     The path to the original image.
    /// </summary>
    public string OriginalImagePath { get; set; } = null!;

    /// <summary>
    ///     The path to the plotted image.
    /// </summary>
    public string PlottedImagePath { get; set; } = null!;
}