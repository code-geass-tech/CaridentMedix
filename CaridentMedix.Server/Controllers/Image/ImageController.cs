using System.Net;
using AutoMapper;
using CaridentMedix.Server.Models;
using Compunet.YoloV8;
using Compunet.YoloV8.Plotting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using Swashbuckle.AspNetCore.Annotations;
using YoloDotNet;
using YoloDotNet.Extensions;
using YoloDotNet.Models;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace CaridentMedix.Server.Controllers.Image;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class ImageController(
    IConfiguration configuration,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : ControllerBase
{
    /// <summary>
    ///     Analyzes an uploaded image using the YOLO model using an alternative library and returns the analysis result.
    /// </summary>
    /// <param name="file">The image file to be analyzed.</param>
    /// <param name="confidence">The confidence to use when analyzing the image.</param>
    /// <param name="iou">The intersection over union to use when analyzing the image.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - BadRequest if the YOLO model is not found.
    ///     - Unauthorized if the user is not found.
    ///     - Ok with the image analysis result if the image is successfully analyzed.
    /// </returns>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> AnalyzeImageAlternativeAsync(
        IFormFile file, float confidence = 0.3f, float iou = 0.45f)
    {
        if (string.IsNullOrEmpty(configuration["YOLO:Model"]))
            return BadRequest("YOLO model not found.");

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var userIdPath = Path.Combine("images", user.Id);
        var basePath = Path.Combine("wwwroot", userIdPath);
        Directory.CreateDirectory(basePath);

        var fileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}_{file.FileName}";

        var path = Path.Combine(basePath, fileName);
        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        using var predictor = YoloV8Predictor.Create(configuration["YOLO:Model"]!);
        predictor.Configuration.Confidence = confidence;
        predictor.Configuration.IoU = iou;

        using var image = await SixLabors.ImageSharp.Image.LoadAsync(file.OpenReadStream());

        var result = await predictor.DetectAsync(image);
        var plot = await result.PlotImageAsync(image);

        var plottedPath = Path.Combine(basePath, $"plotted_{fileName}");
        await using var plotStream = new FileStream(plottedPath, FileMode.Create);
        await plot.SaveAsPngAsync(plotStream);

        var imageResult = new Models.Image
        {
            Name = fileName,
            OriginalImagePath = $"images/{user.Id}/{fileName}",
            PlottedImagePath = $"images/{user.Id}/plotted_{fileName}",
            CreatedAt = DateTimeOffset.UtcNow,
            Width = result.Image.Width,
            Height = result.Image.Height,
            Detections = result.Boxes.Select(box => new Detection
            {
                ClassId = box.Class.Id,
                ClassName = box.Class.Name,
                Confidence = box.Confidence,
                Height = box.Bounds.Height,
                Width = box.Bounds.Width,
                X = box.Bounds.X,
                Y = box.Bounds.Y
            }).ToList()
        };

        await db.Images.AddAsync(imageResult);
        await db.SaveChangesAsync();

        var response = mapper.Map<ImageResponse>(imageResult);

        return Ok(response);
    }

    /// <summary>
    ///     Analyzes an uploaded image using the YOLO model and returns the analysis result.
    /// </summary>
    /// <param name="file">The image file to be analyzed.</param>
    /// <param name="threshold">The threshold to use when analyzing the image.</param>
    /// <param name="iou">The intersection over union to use when analyzing the image.</param>
    /// <param name="drawConfidence">Whether to draw the confidence on the image.</param>
    /// <param name="drawNames">Whether to draw the names on the image.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - BadRequest if the YOLO model is not found.
    ///     - Unauthorized if the user is not found.
    ///     - Ok with the image analysis result if the image is successfully analyzed.
    /// </returns>
    /// z
    [Authorize]
    [HttpPost]
    [SwaggerResponse(Status200OK, "The image analysis result.", typeof(Models.Image))]
    public async Task<IActionResult> AnalyzeImageAsync(IFormFile file,
        double threshold = 0.25, float iou = 0.45f,
        bool drawConfidence = true, bool drawNames = true)
    {
        if (string.IsNullOrEmpty(configuration["YOLO:Model"]))
            return BadRequest("YOLO model not found.");

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var userIdPath = Path.Combine("images", user.Id);
        var basePath = Path.Combine("wwwroot", userIdPath);
        Directory.CreateDirectory(basePath);

        var fileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}_{file.FileName}";

        var path = Path.Combine(basePath, fileName);
        var plottedPath = Path.Combine(basePath, $"plotted_{fileName}");

        await using var stream = new FileStream(path, FileMode.Create);
        await file.CopyToAsync(stream);

        using var yolo = new Yolo(configuration["YOLO:Model"]!, false);
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(file.OpenReadStream());
        var results = yolo.RunObjectDetection(image, threshold, iou);

        results = results.Select(x => new ObjectDetection
        {
            BoundingBox = x.BoundingBox,
            Confidence = x.Confidence,
            Label = new LabelModel
            {
                Index = x.Label.Index,
                Name = x.Label.Name,
                Color = configuration[$"YOLO:Colors:{x.Label.Name}"] ?? x.Label.Color
            }
        }).ToList();

        if (!drawNames)
        {
            image.Draw(results.Select(x => new ObjectDetection
            {
                BoundingBox = x.BoundingBox,
                Confidence = x.Confidence,
                Label = new LabelModel
                {
                    Index = x.Label.Index,
                    Name = string.Empty,
                    Color = x.Label.Color
                }
            }).ToList(), drawConfidence);
        }
        else
            image.Draw(results, drawConfidence);

        await image.SaveAsync(plottedPath);

        var imageResult = new Models.Image
        {
            User = user,
            Name = fileName,
            OriginalImagePath = $"images/{user.Id}/{fileName}",
            PlottedImagePath = $"images/{user.Id}/plotted_{fileName}",
            CreatedAt = DateTimeOffset.UtcNow,
            Width = image.Width,
            Height = image.Height,
            Detections = results.Select(x => new Detection
            {
                ClassId = x.Label.Index,
                ClassName = x.Label.Name,
                Confidence = x.Confidence,
                Height = x.BoundingBox.Height,
                Width = x.BoundingBox.Width,
                X = x.BoundingBox.X,
                Y = x.BoundingBox.Y
            }).ToList()
        };
        await db.Images.AddAsync(imageResult);
        await db.SaveChangesAsync();

        var response = mapper.Map<ImageResponse>(imageResult);

        return Ok(response);
    }

    /// <summary>
    ///     Analyzes a list of uploaded images using the YOLO model and returns the analysis results.
    /// </summary>
    /// <param name="files">The list of image files to be analyzed.</param>
    /// <param name="threshold">The threshold to use when analyzing the images.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - BadRequest if the YOLO model is not found.
    ///     - Unauthorized if the user is not found.
    ///     - Ok with the image analysis results if the images are successfully analyzed.
    /// </returns>
    [Authorize]
    [HttpPost]
    [SwaggerResponse(Status200OK, "The image analysis results.", typeof(List<Models.Image>))]
    public async Task<IActionResult> AnalyzeImagesAsync(List<IFormFile> files, double threshold = 0.25)
    {
        if (string.IsNullOrEmpty(configuration["YOLO:Model"]))
            return BadRequest("YOLO model not found.");

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var userIdPath = Path.Combine("images", user.Id);
        var basePath = Path.Combine("wwwroot", userIdPath);
        Directory.CreateDirectory(basePath);

        var imageResults = new List<Models.Image>();

        foreach (var file in files)
        {
            var fileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}_{file.FileName}";

            var path = Path.Combine(basePath, fileName);
            var plottedPath = Path.Combine(basePath, $"plotted_{fileName}");

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            using var yolo = new Yolo(configuration["YOLO:Model"]!, false);
            using var image = await SixLabors.ImageSharp.Image.LoadAsync(file.OpenReadStream());
            var results = yolo.RunObjectDetection(image, threshold);

            image.Draw(results);
            await image.SaveAsync(plottedPath);

            var imageResult = new Models.Image
            {
                User = user,
                Name = fileName,
                OriginalImagePath = $"images/{user.Id}/{fileName}",
                PlottedImagePath = $"images/{user.Id}/plotted_{fileName}",
                CreatedAt = DateTimeOffset.UtcNow,
                Width = image.Width,
                Height = image.Height,
                Detections = results.Select(x => new Detection
                {
                    ClassId = x.Label.Index,
                    ClassName = x.Label.Name,
                    Confidence = x.Confidence,
                    Height = x.BoundingBox.Height,
                    Width = x.BoundingBox.Width,
                    X = x.BoundingBox.X,
                    Y = x.BoundingBox.Y
                }).ToList()
            };
            await db.Images.AddAsync(imageResult);
            imageResults.Add(imageResult);
        }

        await db.SaveChangesAsync();

        var response = mapper.Map<List<ImageResponse>>(imageResults);

        return Ok(response);
    }

    /// <summary>
    ///     Creates a new report based on the provided image ids and user input.
    /// </summary>
    /// <param name="report">The report data to create.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - BadRequest if one or more image ids are invalid.
    ///     - Unauthorized if the user is not found.
    ///     - Ok with the created report if the report is successfully created.
    /// </returns>
    [Authorize]
    [HttpPost]
    [SwaggerResponse(Status200OK, "The created report.", typeof(DataReportResponse))]
    [SwaggerResponse(Status400BadRequest, "One or more image ids are invalid.", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateReportAsync(DataReportRequest report)
    {
        if (!report.ImageIds.All(x => db.Images.Any(image => image.Id.ToString() == x)))
        {
            return BadRequest(new ErrorResponse
            {
                Message = "One or more image ids are invalid.",
                StatusCode = HttpStatusCode.BadRequest,
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "One or more image ids are invalid.",
                        PropertyName = nameof(report.ImageIds)
                    }
                ]
            });
        }

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var dataReport = new DataReport
        {
            User = user,
            Title = report.Title,
            Description = report.Description,
            CreatedAt = DateTimeOffset.Now,
            Images = await db.Images
               .Where(image => report.ImageIds.Contains(image.Id.ToString()))
               .ToListAsync()
        };

        db.DataReports.Add(dataReport);
        await db.SaveChangesAsync();

        var response = mapper.Map<DataReportResponse>(dataReport);

        return Ok(response);
    }

    /// <summary>
    ///     Deletes a specific report based on the provided id.
    /// </summary>
    /// <param name="id">The id of the report to delete.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - Ok if the report is successfully deleted.
    ///     - Unauthorized if the user is not found or does not match the user associated with the report.
    ///     - NotFound if the report is not found.
    /// </returns>
    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteReport(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var report = await db.DataReports
           .Include(report => report.Images)
           .Include(report => report.User)
           .FirstOrDefaultAsync(report => report.Id == id);

        if (report is null)
            return NotFound();

        if (report.User != user)
            return Unauthorized();

        db.DataReports.Remove(report);
        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    ///     Retrieves all the analyzed images for the current user.
    /// </summary>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - Unauthorized if the user is not found.
    ///     - Ok with the list of analyzed images if the images are successfully retrieved.
    /// </returns>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAnalyzedImages()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var images = await db.Images
           .Where(image => image.User == user)
           .ToListAsync();

        var response = mapper.Map<IEnumerable<ImageResponse>>(images);

        return Ok(response);
    }

    /// <summary>
    ///     Retrieves a specific report based on the provided id.
    /// </summary>
    /// <param name="id">The id of the report to retrieve.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - Ok with the report data if the report is found and the user is authorized.
    ///     - Unauthorized if the user is not found or does not match the user associated with the report.
    ///     - NotFound if the report is not found.
    /// </returns>
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetReport(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var report = await db.DataReports
           .Include(report => report.Images)
           .Include(report => report.User)
           .FirstOrDefaultAsync(report => report.Id == id);

        if (report is null)
            return NotFound();

        if (report.User != user)
            return Unauthorized();

        return Ok(report);
    }

    /// <summary>
    ///     Retrieves reports based on the provided filter.
    /// </summary>
    /// <param name="filter">
    ///     The filter to apply when retrieving reports. This could be a keyword or phrase that is present in
    ///     the report data.
    /// </param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - Unauthorized if the user is not found.
    ///     - Ok with the list of reports that match the filter if the reports are successfully retrieved.
    /// </returns>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> GetReportsAsync(string filter)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var reports = await db.DataReports
           .Where(report => report.User == user)
           .ToListAsync();

        return Ok(reports);
    }
}