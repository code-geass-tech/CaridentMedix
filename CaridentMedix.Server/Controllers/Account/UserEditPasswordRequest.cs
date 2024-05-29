namespace CaridentMedix.Server.Controllers.Account;

public class UserEditPasswordRequest
{
    public required string NewPassword { get; set; }

    public required string OldPassword { get; set; }
}