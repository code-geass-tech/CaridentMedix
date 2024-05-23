using System.Net;
using System.Security.Claims;
using System.Text;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace CaridentMedix.Server.Controllers.Account;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class AccountController(IConfiguration configuration, UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    ///     This method is responsible for adding a user to the Admin role.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the AddUserToAdminRole action.
    ///     If the user is successfully added to the Admin role, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
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
    /// <param name="request">A model containing the user's email and password.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the Login action.
    ///     If the login is successful, it returns an OkResult with the user's token and its expiration time.
    ///     If the login fails, it returns an UnauthorizedResult.
    /// </returns>
    [HttpPost]
    [SwaggerResponse(Status200OK, "A JWT token and its expiration time", typeof(object))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var hasEmail = !string.IsNullOrEmpty(request.Email);
        var hasUsername = !string.IsNullOrEmpty(request.Username);

        if (!hasEmail && !hasUsername)
            return BadRequest("Email or username is required.");

        if (hasEmail && hasUsername)
            return BadRequest("Email and username cannot be used together.");

        var user = hasEmail
            ? await userManager.FindByEmailAsync(request.Email!)
            : await userManager.FindByNameAsync(request.Username!);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
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
    /// <param name="request">A model containing the user's email and password.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the Register action.
    ///     If the registration is successful, it returns an OkResult with a success message.
    ///     If the registration fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Username
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (result.Succeeded)
            return Ok(new BaseResponse { Message = "User created successfully!" });

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for registering a new dentist.
    /// </summary>
    /// <param name="request">A model containing the dentist's email, name, password, phone number, and username.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the RegisterDentist action.
    ///     If the registration is successful, it returns an OkResult with a success message.
    ///     If the registration fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    public async Task<IActionResult> RegisterDentistAsync([FromBody] RegisterDentistRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Username,
            Email = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        user.Dentist = new Dentist
        {
            Email = request.Email,
            Name = request.Name,
            PhoneNumber = request.PhoneNumber
        };

        await userManager.UpdateAsync(user);
        return Ok(new BaseResponse { Message = "Dentist created successfully!" });
    }

    /// <summary>
    ///     This method is responsible for uploading an avatar for a user.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <param name="avatar">The avatar file to be uploaded.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the UploadAvatar action.
    ///     If the upload is successful, it returns an OkResult with a success message.
    ///     If the upload fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost("{userId}")]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(ErrorResponse))]
    public async Task<IActionResult> UploadAvatarAsync(string userId, [FromForm] IFormFile avatar)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var fileName = Path.GetRandomFileName() + Path.GetExtension(avatar.FileName);
        var filePath = Path.Combine("wwwroot", "avatars", fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await avatar.CopyToAsync(stream);
        }

        user.ImagePath = $"/avatars/{fileName}";
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded) return Ok(new BaseResponse { Message = "Avatar uploaded successfully!" });

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for verifying a token.
    /// </summary>
    /// <param name="token">The token to be verified.</param>
    /// <returns>An IActionResult that represents the result of the VerifyToken action.</returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "The token is valid", typeof(TokenValidationResult))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "The token is invalid", typeof(ErrorResponse))]
    public async Task<IActionResult> VerifyTokenAsync(string token)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
        };

        try
        {
            var result = await tokenHandler.ValidateTokenAsync(token, tokenValidationParameters);
            if (result is { IsValid: false })
            {
                return Unauthorized(new ErrorResponse
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    Message = "The token is invalid",
                    Details =
                    [
                        new ErrorDetail
                        {
                            Message = "The token is invalid",
                            PropertyName = nameof(token)
                        }
                    ]
                });
            }

            var userId = (string?) result.Claims.FirstOrDefault(claim => claim.Key == ClaimTypes.NameIdentifier).Value;
            if (userId is null)
            {
                return BadRequest(new ErrorResponse
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = "The token is invalid",
                    Details =
                    [
                        new ErrorDetail
                        {
                            Message = "The token is invalid",
                            PropertyName = nameof(token)
                        }
                    ]
                });
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
            {
                return NotFound(new ErrorResponse
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Message = "The user was not found",
                    Details =
                    [
                        new ErrorDetail
                        {
                            Message = "The user was not found",
                            PropertyName = nameof(userId)
                        }
                    ]
                });
            }

            var expirationClaim = (long) result.Claims.FirstOrDefault(claim => claim.Key == JwtRegisteredClaimNames.Exp).Value;
            var expirationDate = DateTimeOffset.FromUnixTimeSeconds(expirationClaim).UtcDateTime;

            return Ok(new TokenValidationResult
            {
                IsValid = true,
                Message = "The token is valid",
                Expiration = expirationDate,
                Claims = result.Claims
            });
        }
        catch (Exception e)
        {
            return BadRequest(e.ToErrorResponse());
        }
    }
}