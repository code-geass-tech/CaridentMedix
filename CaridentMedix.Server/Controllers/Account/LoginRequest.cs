namespace CaridentMedix.Server.Controllers.Account;

public class LoginRequest
{
    public string Password { get; init; } = null!;

    public string? Email { get; init; }

    public string? Username { get; init; }
}