using System.Net;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using CaridentMedix.Server.Controllers.Admin;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Annotations;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace CaridentMedix.Server.Controllers.Account;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class AccountController(
    IConfiguration configuration,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : ControllerBase
{
    /// <summary>
    ///     This method is responsible for changing a user's name.
    /// </summary>
    /// <param name="name">The new name to be set for the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the ChangeName action.
    ///     If the change is successful, it returns an OkResult with a success message.
    ///     If the change fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPut]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    public async Task<IActionResult> ChangeNameAsync(string name)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        user.Name = name;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? Ok(new BaseResponse { Message = "Name changed successfully!" })
            : BadRequest(result.ToErrorResponse());
    }

    /// <summary>
    ///     This method is responsible for creating an appointment.
    /// </summary>
    /// <param name="request">A model containing the appointment's information.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the CreateAppointment action.
    ///     If the creation is successful, it returns an OkResult with the appointment's information.
    ///     If the creation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var clinic = await db.Clinics
           .Include(clinic => clinic.Appointments)
           .FirstOrDefaultAsync(clinic => clinic.Id == request.ClinicId)
           .ConfigureAwait(false);

        if (clinic is null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The clinic was not found",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The clinic was not found",
                        PropertyName = nameof(request.ClinicId)
                    }
                ]
            });
        }

        var dentist = clinic.Dentists.FirstOrDefault(dentist => dentist.Id == request.DentistId);

        var appointment = new Appointment
        {
            User = user,
            Clinic = clinic,
            CreatedAt = DateTimeOffset.UtcNow,
            ScheduledAt = request.ScheduledAt.ToUniversalTime(),
            Dentist = dentist
        };

        await db.Appointments.AddAsync(appointment);
        await db.SaveChangesAsync();

        var response = mapper.Map<AppointmentModel>(appointment);

        return Ok(response);
    }

    /// <summary>
    ///     This method is responsible for deleting a user's avatar.
    /// </summary>
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
        if (user is null) return NotFound();

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
    ///     This method is responsible for editing a user's password.
    /// </summary>
    /// <param name="request">A model containing the user's old and new passwords.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the EditPassword action.
    ///     If the edit is successful, it returns an OkResult with a success message.
    ///     If the edit fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPut]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    public async Task<IActionResult> EditPasswordAsync(SelfUserEditPasswordRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
        return result.Succeeded
            ? Ok(new BaseResponse { Message = "Password changed successfully!" })
            : BadRequest(result.ToErrorResponse());
    }

    /// <summary>
    ///     This method is responsible for editing a user's information.
    /// </summary>
    /// <param name="request">A model containing the user's new information.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the EditSelf action.
    ///     If the edit is successful, it returns an OkResult with a success message.
    ///     If the edit fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPut]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(ErrorResponse))]
    public async Task<IActionResult> EditSelfAsync(SelfUserEditRequest request)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        user.Name = request.Name ?? user.Name;

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? Ok(new BaseResponse { Message = "User updated successfully!" })
            : BadRequest(result.ToErrorResponse());
    }

    /// <summary>
    ///     This method is responsible for getting the currently authenticated user.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetSelf action.
    ///     If the retrieval is successful, it returns an OkResult with the user's information.
    ///     If the retrieval fails, it returns a NotFoundObjectResult with an error message.
    /// </returns>
    [HttpGet]
    [Authorize]
    [SwaggerResponse(Status200OK, "The authenticated user's information", typeof(GetSelfResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(ErrorResponse))]
    public async Task<IActionResult> GetSelfAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var response = mapper.Map<GetSelfResponse>(user);
        return Ok(response);
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
            Expires = DateTime.Now.AddDays(7),
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
            UserName = request.Email,
            Name = request.Name
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
        if (user is null) return NotFound();

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