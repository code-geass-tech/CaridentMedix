namespace CaridentMedix.Server.Controllers.Clinic;

public class RegisterDentistRequest
{
    public string Email { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string PhoneNumber { get; init; } = null!;
}