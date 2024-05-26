using System.Net;
using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace CaridentMedix.Server.Controllers;

/// <summary>
///     An error object returned for failed requests
/// </summary>
[SwaggerSchema(Title = "Error response")]
public class ErrorResponse : BaseResponse
{
    /// <summary>
    ///     The HTTP status code of the response
    /// </summary>
    [JsonIgnore]
    public required HttpStatusCode StatusCode { get; set; }

    /// <summary>
    ///     Specific details about what caused the error
    /// </summary>
    public required List<ErrorDetail> Details { get; set; } = [];
}