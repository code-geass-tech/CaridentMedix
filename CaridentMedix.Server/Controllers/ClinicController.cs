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
        if (db.Clinics.Any(c => c.Name == clinic.Name))
            return BadRequest("A clinic with the same name already exists.");

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

        if (clinic.Users.Any(x => x.Id == user.Id) && !await userManager.IsInRoleAsync(currentUser, "Admin"))
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
    [HttpGet]
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
    /// <param name="name">A specific search term for the name of a clinic or dentist.</param>
    /// <param name="email">A specific search term for the email of a clinic or dentist.</param>
    /// <param name="phoneNumber">A specific search term for the phone number of a clinic.</param>
    /// <param name="address">A specific search term for the address of a clinic.</param>
    /// <param name="description">A specific search term for the description of a clinic.</param>
    /// <param name="website">A specific search term for the website of a clinic.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation. The task result contains an IActionResult that can be one of the
    ///     following:
    ///     - A result that represents a status code 200 (OK) with a list of clinics.
    ///     - A result that represents a status code 500 (Internal Server Error) if an exception was thrown.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> GetClinicsAsync(
        string? generalSearch, string? name, string? email, string? phoneNumber,
        string? address, string? description, string? website)
    {
        const int maxDistance = 3;

        var clinicsQuery = db.Clinics
            .Include(clinic => clinic.Dentists)
            .AsEnumerable();

        if (!string.IsNullOrEmpty(generalSearch))
        {
            clinicsQuery = clinicsQuery.Where(clinic
                => clinic.Name.StartsWith(generalSearch)
                || LevenshteinDistance(clinic.Name, generalSearch) <= maxDistance
                || clinic.Email!.StartsWith(generalSearch)
                || LevenshteinDistance(clinic.Email!, generalSearch) <= maxDistance
                || clinic.PhoneNumber!.StartsWith(generalSearch)
                || LevenshteinDistance(clinic.PhoneNumber!, generalSearch) <= maxDistance
                || clinic.Dentists.Any(dentist
                    => dentist.Name.StartsWith(generalSearch)
                    || LevenshteinDistance(dentist.Name, generalSearch) <= maxDistance)
                || clinic.Dentists.Any(dentist
                    => dentist.Email!.StartsWith(generalSearch)
                    || LevenshteinDistance(dentist.Email!, generalSearch) <= maxDistance)
                || clinic.Dentists.Any(dentist
                    => dentist.PhoneNumber!.StartsWith(generalSearch)
                    || LevenshteinDistance(dentist.PhoneNumber!, generalSearch) <= maxDistance)
                || clinic.Address.StartsWith(generalSearch)
                || LevenshteinDistance(clinic.Address, generalSearch) <= maxDistance
                || clinic.Description!.StartsWith(generalSearch)
                || LevenshteinDistance(clinic.Description!, generalSearch) <= maxDistance
                || clinic.Website!.StartsWith(generalSearch)
                || LevenshteinDistance(clinic.Website!, generalSearch) <= maxDistance);
        }

        if (!string.IsNullOrEmpty(name))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Name.StartsWith(name) || LevenshteinDistance(clinic.Name, name) <= maxDistance
                || clinic.Dentists.Any(dentist
                    => dentist.Name.StartsWith(name) ||
                    LevenshteinDistance(dentist.Name, name) <= maxDistance));
        }

        if (!string.IsNullOrEmpty(email))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Email!.StartsWith(email) || LevenshteinDistance(clinic.Email!, email) <= maxDistance
                || clinic.Dentists.Any(dentist
                    => dentist.Email!.StartsWith(email) ||
                    LevenshteinDistance(dentist.Email!, email) <= maxDistance));
        }

        if (!string.IsNullOrEmpty(phoneNumber))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.PhoneNumber!.StartsWith(phoneNumber) ||
                LevenshteinDistance(clinic.PhoneNumber!, phoneNumber) <= maxDistance
                || clinic.Dentists.Any(dentist
                    => dentist.PhoneNumber!.StartsWith(phoneNumber) ||
                    LevenshteinDistance(dentist.PhoneNumber!, phoneNumber) <= maxDistance));
        }

        if (!string.IsNullOrEmpty(address))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Address.StartsWith(address) ||
                LevenshteinDistance(clinic.Address, address) <= maxDistance);
        }

        if (!string.IsNullOrEmpty(description))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Description!.StartsWith(description) ||
                LevenshteinDistance(clinic.Description!, description) <= maxDistance);
        }

        if (!string.IsNullOrEmpty(website))
        {
            clinicsQuery = clinicsQuery.Where(clinic =>
                clinic.Website!.StartsWith(website) ||
                LevenshteinDistance(clinic.Website!, website) <= maxDistance);
        }

        var clinics = clinicsQuery.Select(mapper.Map<ClinicModel>).ToList();
        var clinicModels = string.IsNullOrEmpty(generalSearch)
            ? clinics
            : clinics
                .Select(mapper.Map<ClinicModel>)
                .OrderBy(clinic =>
                    WeightedLevenshteinDistance(clinic.Name, generalSearch, 0.5)
                    + WeightedLevenshteinDistance(clinic.Email!, generalSearch, 1.5)
                    + WeightedLevenshteinDistance(clinic.PhoneNumber!, generalSearch, 1.5)
                    + WeightedLevenshteinDistance(clinic.Address, generalSearch, 1.5)
                    + WeightedLevenshteinDistance(clinic.Description!, generalSearch, 1.5)
                    + WeightedLevenshteinDistance(clinic.Website!, generalSearch, 1.5)
                    + WeightedLevenshteinDistance(clinic.Name, name, 0.5)
                    + WeightedLevenshteinDistance(clinic.Email!, email, 1.5)
                    + WeightedLevenshteinDistance(clinic.PhoneNumber!, phoneNumber, 1.5)
                    + WeightedLevenshteinDistance(clinic.Address, address, 1.5)
                    + WeightedLevenshteinDistance(clinic.Description!, description, 1.5)
                    + WeightedLevenshteinDistance(clinic.Website!, website, 1.5))
                .ToList();

        return Ok(clinicModels);

        static int WeightedLevenshteinDistance(string source, string? target, double weight)
        {
            if (string.IsNullOrEmpty(target)) return 0;

            var distance = LevenshteinDistance(source, target);
            return (int) (distance * weight);
        }
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return string.IsNullOrEmpty(target) ? 0 : target.Length;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var distance = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; distance[i, 0] = i++) { }
        for (var j = 0; j <= target.Length; distance[0, j] = j++) { }

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost);
            }
        }

        return distance[source.Length, target.Length];
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