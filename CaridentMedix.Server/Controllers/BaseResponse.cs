namespace CaridentMedix.Server.Controllers;

/// <summary>
///     Base class for all responses
/// </summary>
public class BaseResponse
{
    /// <summary>
    ///     The message associated with the response
    /// </summary>
    public string Message { get; init; } = null!;
}