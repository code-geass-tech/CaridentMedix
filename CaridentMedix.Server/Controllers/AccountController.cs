using System.Security.Claims;
using System.Text;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CaridentMedix.Server.Controllers;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class AccountController(IConfiguration configuration, UserManager<ApplicationUser> userManager) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddUserToAdminRoleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for logging in a user.
    /// </summary>
    /// <param name="model">A model containing the user's email and password.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the Login action.
    ///     If the login is successful, it returns an OkResult with the user's token and its expiration time.
    ///     If the login fails, it returns an UnauthorizedResult.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> LoginAsync([FromBody] LoginModel model)
    {
        var user = await userManager.FindByEmailAsync(model.Username);
        if (user is null || !await userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized();

        var authClaims = new[]
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(authClaims),
            Expires = DateTime.Now.AddHours(2),
            Audience = configuration["Jwt:Audience"],
            Issuer = configuration["Jwt:Issuer"],
            SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha512)
        };

        var tokenHandler = new JsonWebTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new
        {
            Token = token,
            Expiration = tokenDescriptor.Expires
        });
    }

    /// <summary>
    ///     This method is responsible for registering a new user.
    /// </summary>
    /// <param name="model">A model containing the user's email and password.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the Register action.
    ///     If the registration is successful, it returns an OkResult with a success message.
    ///     If the registration fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Username
        };

        var result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
            return Ok(new { Message = "User created successfully!" });
        return BadRequest(result.Errors);
    }
}

public class RegisterModel
{
    public string Password { get; set; }

    public string Username { get; set; }
}

public class LoginModel
{
    public string Password { get; set; }

    public string Username { get; set; }
}