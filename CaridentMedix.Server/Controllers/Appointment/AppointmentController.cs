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

        if (request.DentistId is not null && dentist is null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The dentist was not found",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The dentist was not found",
                        PropertyName = nameof(request.DentistId)
                    }
                ]
            });
        }

        var appointment = new Models.Appointment
        {
            User = user,
            Clinic = clinic,
            CreatedAt = DateTimeOffset.UtcNow,
            ScheduledAt = request.ScheduledAt.ToUniversalTime(),
            Dentist = dentist,
            Status = "Pending",
            ClinicMessage = "Please wait until your appointment is confirmed."
        };

        await db.Appointments.AddAsync(appointment);
        await db.SaveChangesAsync();

        var response = mapper.Map<AppointmentModel>(appointment);

        return Ok(response);
    }

    /// <summary>
    ///     This method is responsible for deleting an appointment.
    /// </summary>
    /// <param name="id">The id of the appointment to delete.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the DeleteAppointment action.
    ///     If the deletion is successful, it returns an OkResult.
    ///     If the deletion fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> DeleteAppointmentAsync(int id)
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

        var appointment = await db.Appointments
           .Include(appointment => appointment.Clinic)
           .Include(appointment => appointment.Dentist)
           .FirstOrDefaultAsync(appointment => appointment.Id == id)
           .ConfigureAwait(false);

        if (appointment is null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The appointment was not found",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The appointment was not found",
                        PropertyName = nameof(id)
                    }
                ]
            });
        }

        if (appointment.User.Id != user.Id)
        {
            return Unauthorized(new ErrorResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "The user is not authorized to delete the appointment",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user is not authorized to delete the appointment",
                        PropertyName = nameof(User)
                    }
                ]
            });
        }

        if (appointment.Status != "Pending")
        {
            return BadRequest(new ErrorResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "The appointment cannot be deleted",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The appointment cannot be deleted",
                        PropertyName = nameof(appointment.Status)
                    }
                ]
            });
        }

        if (appointment.User.Id != user.Id)
        {
            return Unauthorized(new ErrorResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "The user is not authorized to delete the appointment",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user is not authorized to delete the appointment",
                        PropertyName = nameof(User)
                    }
                ]
            });
        }

        db.Appointments.Remove(appointment);
        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    ///     This method is responsible for getting an appointment.
    /// </summary>
    /// <param name="id">The id of the appointment to get.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAppointment action.
    ///     If the retrieval is successful, it returns an OkResult with the appointment's information.
    ///     If the retrieval fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpGet]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAppointmentAsync(int id)
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

        var appointment = await db.Appointments
           .Include(appointment => appointment.Clinic)
           .Include(appointment => appointment.Dentist)
           .FirstOrDefaultAsync(appointment => appointment.Id == id)
           .ConfigureAwait(false);

        if (appointment is null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The appointment was not found",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The appointment was not found",
                        PropertyName = nameof(id)
                    }
                ]
            });
        }

        if (appointment.User.Id != user.Id && user.Clinic is not null && user.Clinic.Id != appointment.Clinic.Id)
        {
            return Unauthorized(new ErrorResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "The user is not authorized to view the appointment",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user is not authorized to view the appointment",
                        PropertyName = nameof(User)
                    }
                ]
            });
        }

        var response = mapper.Map<AppointmentModel>(appointment);

        return Ok(response);
    }

    /// <summary>
    ///     This method is responsible for getting all the appointments of the current user.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAppointments action.
    ///     If the retrieval is successful, it returns an OkResult with the appointments' information.
    ///     If the retrieval fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpGet]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAppointmentsAsync()
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

        var appointments = await db.Appointments
           .Include(appointment => appointment.Clinic)
           .Include(appointment => appointment.Dentist)
           .Where(appointment => appointment.User.Id == user.Id || (user.Clinic != null && appointment.Clinic.Id == user.Clinic.Id))
           .ToListAsync()
           .ConfigureAwait(false);

        var response = mapper.Map<IEnumerable<AppointmentModel>>(appointments);

        return Ok(response);
    }

    /// <summary>
    ///     This method is responsible for updating an appointment.
    /// </summary>
    /// <param name="appointmentId">The id of the appointment to update.</param>
    /// <param name="request">A model containing the appointment's updated information.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the UpdateAppointment action.
    ///     If the update is successful, it returns an OkResult with the appointment's information.
    ///     If the update fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPatch("{appointmentId:int}")]
    [Authorize]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "A bad request response", typeof(ErrorResponse))]
    [SwaggerResponse(Status401Unauthorized, "An unauthorized response", typeof(ErrorResponse))]
    public async Task<IActionResult> UpdateAppointmentAsync(int appointmentId, UpdateAppointmentRequest request)
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

        var appointment = await db.Appointments
           .Include(appointment => appointment.Clinic)
           .Include(appointment => appointment.Dentist)
           .FirstOrDefaultAsync(appointment => appointment.Id == appointmentId)
           .ConfigureAwait(false);

        if (appointment is null)
        {
            return NotFound(new ErrorResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Message = "The appointment was not found",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The appointment was not found",
                        PropertyName = nameof(appointmentId)
                    }
                ]
            });
        }

        if (!string.IsNullOrEmpty(request.UserMessage) && appointment.User.Id != user.Id)
        {
            return Unauthorized(new ErrorResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "The user is not authorized to update the appointment",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user is not authorized to update the appointment",
                        PropertyName = nameof(User)
                    }
                ]
            });
        }

        if (!string.IsNullOrEmpty(request.ClinicMessage) && user.Clinic is null)
        {
            return Unauthorized(new ErrorResponse
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Message = "The user is not authorized to update the appointment",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The user is not authorized to update the appointment",
                        PropertyName = nameof(User)
                    }
                ]
            });
        }

        if (appointment.Status != "Pending")
        {
            return BadRequest(new ErrorResponse
            {
                StatusCode = HttpStatusCode.BadRequest,
                Message = "The appointment cannot be updated",
                Details =
                [
                    new ErrorDetail
                    {
                        Message = "The appointment cannot be updated",
                        PropertyName = nameof(appointment.Status)
                    }
                ]
            });
        }

        appointment.ScheduledAt = request.ScheduledAt?.ToUniversalTime() ?? appointment.ScheduledAt;
        appointment.Status = request.Status ?? appointment.Status;
        appointment.ClinicMessage = request.ClinicMessage ?? appointment.ClinicMessage;
        appointment.UserMessage = request.UserMessage ?? appointment.UserMessage;

        await db.SaveChangesAsync();

        var response = mapper.Map<AppointmentModel>(appointment);

        return Ok(response);
    }
}