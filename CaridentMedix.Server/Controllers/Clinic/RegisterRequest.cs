namespace CaridentMedix.Server.Controllers.Clinic;

public class RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}