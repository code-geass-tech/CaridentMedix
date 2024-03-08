using AutoMapper;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaridentMedix.Server.Controllers;

/// <summary>
///     ClinicController is a controller that handles operations related to clinics.
/// </summary>
[ApiController]
[Route("[controller]/[action]")]
public class ClinicController(
    IConfiguration configuration,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : ControllerBase
{
    /// <summary>
    ///     Adds a new clinic asynchronously.
    /// </summary>
    /// <param name="clinic">The clinic model to be added.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the
    ///     following:
    ///     - A result that represents status code 200 (OK) with the added clinic.
    ///     - A result that represents status code 500 (Internal Server Error) if an exception was thrown.
    /// </returns>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddClinicAsync(ClinicModel clinic)
    {
        var newClinic = mapper.Map<Clinic>(clinic);

        db.Clinics.Add(newClinic);
        await db.SaveChangesAsync();

        return Ok(clinic);
    }

    /// <summary>
    ///     Asynchronously adds a user to the clinic admin role.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="clinicId">The clinic's unique identifier.</param>
    /// <returns>An IActionResult that represents the result of the AddUserToClinicAdmin action.</returns>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddUserToClinicAdminAsync(string userId, int clinicId)
    {
        var currentUser = await userManager.GetUserAsync(User);
        if (currentUser is null) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var clinic = await db.Clinics
            .Include(clinic => clinic.Users)
            .FirstOrDefaultAsync(clinic => clinic.Id == clinicId);
        if (clinic is null) return NotFound();

        if (clinic.Users!.Any(x => x.Id == user.Id) && !await userManager.IsInRoleAsync(currentUser, "Admin"))
            return Unauthorized();

        clinic.Users.Add(user);
        await db.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    ///     Asynchronously retrieves a list of all clinics.
    /// </summary>
    /// <param name="latitude">The latitude of the user's location.</param>
    /// <param name="longitude">The longitude of the user's location.</param>
    /// <returns>The list of clinics ordered by distance from the user's location.</returns>
    public async Task<IActionResult> FindNearbyClinics(float latitude, float longitude)
    {
        var clinics = await db.Clinics.ToListAsync();
        var clinicModels = clinics.Select(mapper.Map<ClinicModel>).ToList();

        foreach (var clinic in clinicModels)
        {
            clinic.Distance = Distance(clinic.Longitude, clinic.Latitude);
        }

        clinicModels = clinicModels.OrderBy(clinic => clinic.Distance).ToList();

        return Ok(clinicModels);

        double Distance(float clinicLongitude, float clinicLatitude)
            => Math.Sqrt(Math.Pow(clinicLongitude - longitude, 2) + Math.Pow(clinicLatitude - latitude, 2));
    }

    /// <summary>
    ///     Asynchronously retrieves a list of clinics based on the provided search parameters.
    /// </summary>
    /// <param name="generalSearch">A general search term that is used to search across multiple fields.</param>
    /// <param name="dentistName">A specific search term for the name of a dentist associated with a clinic.</param>
    /// <param name="name">A specific search term for the name of a clinic.</param>
    /// <param name="address">A specific search term for the address of a clinic.</param>
    /// <param name="description">A specific search term for the description of a clinic.</param>
    /// <param name="email">A specific search term for the email of a clinic.</param>
    /// <param name="imagePath">A specific search term for the image path of a clinic.</param>
    /// <param name="phoneNumber">A specific search term for the phone number of a clinic.</param>
    /// <param name="website">A specific search term for the website of a clinic.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the
    ///     following:
    ///     - A result that represents a status code 200 (OK) with a list of clinics.
    ///     - A result that represents a status code 500 (Internal Server Error) if an exception was thrown.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> GetClinicsAsync(string? generalSearch, string? dentistName, string? name,
        string? address, string? description, string? email, string? imagePath, string? phoneNumber, string? website)
    {
        var clinicsQuery = db.Clinics.AsQueryable();

        if (!string.IsNullOrEmpty(generalSearch))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Name.Contains(generalSearch) ||
                clinic.Address.Contains(generalSearch) ||
                clinic.Description!.Contains(generalSearch) ||
                clinic.Email!.Contains(generalSearch) ||
                clinic.ImagePath!.Contains(generalSearch) ||
                clinic.PhoneNumber!.Contains(generalSearch) ||
                clinic.Website!.Contains(generalSearch));
        }

        if (!string.IsNullOrEmpty(dentistName))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Dentists.Any(dentist => dentist.Name == dentistName));
        }

        if (!string.IsNullOrEmpty(name))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Name.Contains(name));

        if (!string.IsNullOrEmpty(address))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Address.Contains(address));

        if (!string.IsNullOrEmpty(description))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Description!.Contains(description));

        if (!string.IsNullOrEmpty(email))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Email!.Contains(email));

        if (!string.IsNullOrEmpty(imagePath))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.ImagePath!.Contains(imagePath));

        if (!string.IsNullOrEmpty(phoneNumber))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.PhoneNumber!.Contains(phoneNumber));

        if (!string.IsNullOrEmpty(website))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Website!.Contains(website));

        var clinics = await clinicsQuery.ToListAsync();
        var clinicModels = clinics.Select(mapper.Map<ClinicModel>).ToList();

        return Ok(clinicModels);
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ClinicModel
    {
        public double Distance { get; set; }

        public float Latitude { get; init; }

        public float Longitude { get; init; }

        public List<DentistModel> Dentists { get; init; } = [];

        public string Address { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string? Description { get; init; } = null!;

        public string? Email { get; init; } = null!;

        public string? ImagePath { get; init; } = null!;

        public string? PhoneNumber { get; init; } = null!;

        public string? Website { get; init; } = null!;
    }

    public class DentistModel
    {
        public string Email { get; init; } = null!;

        public string Name { get; init; } = null!;

        public string PhoneNumber { get; init; } = null!;
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}