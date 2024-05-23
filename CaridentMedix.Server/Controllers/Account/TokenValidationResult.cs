namespace CaridentMedix.Server.Controllers.Account;

public class TokenValidationResult : BaseResponse
{
    public bool IsValid { get; init; }

    public DateTimeOffset? Expiration { get; init; }

    public IDictionary<string, object> Claims { get; init; } = new Dictionary<string, object>();
}