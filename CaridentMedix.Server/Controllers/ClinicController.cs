using AutoMapper;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CaridentMedix.Server.Controllers;

/// <inheritdoc />
[ApiController]
[Route("[controller]/[action]")]
public class ClinicController(
    IConfiguration configuration,
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext db) : ControllerBase
{
    // [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> AddClinicAsync(ClinicModel clinic)
    {
        var newClinic = mapper.Map<Clinic>(clinic);

        db.Clinics.Add(newClinic);
        await db.SaveChangesAsync();

        return Ok(clinic);
    }

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

        if (!string.IsNullOrEmpty(name)) clinicsQuery = clinicsQuery.Where(clinic => clinic.Name.Contains(name));

        if (!string.IsNullOrEmpty(address))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Address.Contains(address));

        if (!string.IsNullOrEmpty(description))
            clinicsQuery = clinicsQuery.Where(clinic => clinic.Description!.Contains(description));

        if (!string.IsNullOrEmpty(email)) clinicsQuery = clinicsQuery.Where(clinic => clinic.Email!.Contains(email));

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