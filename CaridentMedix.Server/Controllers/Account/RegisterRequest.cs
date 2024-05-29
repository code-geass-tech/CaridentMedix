namespace CaridentMedix.Server.Controllers.Account;

public class RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }

    public string? Name { get; set; }
}