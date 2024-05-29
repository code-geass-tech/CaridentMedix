namespace CaridentMedix.Server.Controllers.Account;

public class SelfUserEditPasswordRequest
{
    public required string NewPassword { get; set; }

    public required string OldPassword { get; set; }
}