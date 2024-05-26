namespace CaridentMedix.Server.Controllers.Account;

public class LoginResponse : BaseResponse
{
    public string Token { get; init; } = null!;

    public DateTime? Expiration { get; init; }
}