namespace CaridentMedix.Server.Controllers.Clinic;

public class UpdateDentistRequest
{
    public string? Email { get; set; }
 
    public string? Name { get; set; }
 
    public string? PhoneNumber { get; set; }

    public int? ClinicId { get; set; }
}