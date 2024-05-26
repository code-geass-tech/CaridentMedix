using System.Net;
using System.Security.Claims;
using System.Text;
using AutoMapper;
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
public class AccountController(IConfiguration configuration, IMapper mapper, UserManager<ApplicationUser> userManager) : ControllerBase
{
    /// <summary>
    ///     This method is responsible for deleting a user's avatar.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the DeleteAvatar action.
    ///     If the deletion is successful, it returns an OkResult with a success message.
    ///     If the deletion fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAvatarAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(user.ImagePath))
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The user does not have an avatar",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user does not have an avatar",
                        PropertyName = nameof(user)
                    }
                ]
            });
        }

        System.IO.File.Delete(user.ImagePath);
        user.ImagePath = null;
        var result = await userManager.UpdateAsync(user);

        if (result.Succeeded) return Ok(new BaseResponse { Message = "Avatar deleted successfully!" });

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for getting a user's avatar.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAvatar action.
    ///     If the retrieval is successful, it returns an OkResult with the user's avatar.
    ///     If the retrieval fails, it returns a NotFoundObjectResult with an error message.
    /// </returns>
    [HttpGet]
    [Authorize]
    [SwaggerResponse(Status200OK, "The user's avatar", typeof(AvatarResult))]
    [SwaggerResponse(Status404NotFound, "The user does not have an avatar", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAvatarAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(user.ImagePath))
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The user does not have an avatar",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user does not have an avatar",
                        PropertyName = nameof(user)
                    }
                ]
            });
        }

        return Ok(new AvatarResult
        {
            Message = "Avatar retrieved successfully",
            Avatar = user.ImagePath
        });
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
    [SwaggerResponse(Status200OK, "A JWT token and its expiration time", typeof(LoginResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        var user = await userManager.FindByEmailAsync(request.Email);

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

        return Ok(new LoginResponse
        {
            Message = "Successfully logged in",
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
    [SwaggerResponse(Status200OK, "A success message", typeof(RegisterResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(result.ToErrorResponse());

        return Ok(new RegisterResponse
        {
            Message = "User created successfully!"
        });
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
    [HttpPost]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(ErrorResponse))]
    public async Task<IActionResult> UploadAvatarAsync(IFormFile avatar)
    {
        var user = await userManager.GetUserAsync(User);
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