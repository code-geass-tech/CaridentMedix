namespace CaridentMedix.Server.Controllers.Account;

public class RegisterDentistRequest
{
    public string Email { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Password { get; init; } = null!;

    public string PhoneNumber { get; init; } = null!;

    public string Username { get; init; } = null!;
}