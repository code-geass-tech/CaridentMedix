using System.Net;
using AutoMapper;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace CaridentMedix.Server.Controllers.Appointment;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class AppointmentController(
    ILogger<AppointmentController> logger,
    IConfiguration configuration,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : ControllerBase
{
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
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateAppointmentAsync(CreateAppointmentRequest request)
    {
        var user = await userManager.GetUserAsync(User);
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
                        PropertyName = nameof(User)
                    }
                ]
            });
        }

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

        var appointment = new Models.Appointment
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
}