namespace CaridentMedix.Server.Controllers.Account;

public class RegisterRequest
{
    public string Email { get; init; } = null!;

    public string Password { get; init; } = null!;

    public string Username { get; init; } = null!;
}