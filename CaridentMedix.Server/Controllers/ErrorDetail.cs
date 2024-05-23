using Swashbuckle.AspNetCore.Annotations;

namespace CaridentMedix.Server.Controllers;

/// <summary>
///     Details of an invalid request property
/// </summary>
[SwaggerSchema(Title = "Details of an invalid request property")]
public class ErrorDetail
{
    /// <summary>
    ///     The error message associated with the property
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    ///     The name of the property that caused the error
    /// </summary>
    public required string PropertyName { get; set; }
}