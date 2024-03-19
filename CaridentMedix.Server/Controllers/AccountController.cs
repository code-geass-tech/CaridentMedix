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
        var hasEmail = !string.IsNullOrEmpty(model.Email);
        var hasUsername = !string.IsNullOrEmpty(model.Username);

        if (!hasEmail && !hasUsername)
            return BadRequest("Email or username is required.");

        if (hasEmail && hasUsername)
            return BadRequest("Email and username cannot be used together.");

        var user = hasEmail
            ? await userManager.FindByEmailAsync(model.Email!)
            : await userManager.FindByNameAsync(model.Username!);

        if (user is null || !await userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized("Invalid email or password.");

        var roles = await userManager.GetRolesAsync(user);
        var authClaims = new List<Claim>(roles.Select(role => new Claim(ClaimTypes.Role, role)))
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
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

    /// <summary>
    ///     This method is responsible for registering a new dentist.
    /// </summary>
    /// <param name="model">A model containing the dentist's email, name, password, phone number, and username.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the RegisterDentist action.
    ///     If the registration is successful, it returns an OkResult with a success message.
    ///     If the registration fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    public async Task<IActionResult> RegisterDentistAsync([FromBody] RegisterDentistModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        user.Dentist = new Dentist
        {
            Email = model.Email,
            Name = model.Name,
            PhoneNumber = model.PhoneNumber
        };

        await userManager.UpdateAsync(user);
        return Ok(new { Message = "Dentist created successfully!" });
    }

    [HttpPost("{userId}")]
    public async Task<IActionResult> UploadAvatarAsync(string userId, [FromForm] IFormFile avatar)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var fileName = Path.GetRandomFileName() + Path.GetExtension(avatar.FileName);
        var filePath = Path.Combine("wwwroot", "avatars", fileName);

        using (var stream = System.IO.File.Create(filePath))
        {
            await avatar.CopyToAsync(stream);
        }

        user.ImagePath = $"/avatars/{fileName}";
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            return Ok(new { Message = "Avatar uploaded successfully!" });
        }

        return BadRequest(result.Errors);
    }
}

public class RegisterDentistModel
{
    public string Email { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Password { get; init; } = null!;

    public string PhoneNumber { get; init; } = null!;

    public string Username { get; init; } = null!;
}

public class RegisterModel
{
    public string Email { get; init; } = null!;

    public string Password { get; init; } = null!;

    public string Username { get; init; } = null!;
}

public class LoginModel
{
    public string Password { get; init; } = null!;

    public string? Email { get; init; }

    public string? Username { get; init; }
}