using AutoMapper;
using CaridentMedix.Server.Controllers.Clinic;
using CaridentMedix.Server.Controllers.Image;
using CaridentMedix.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace CaridentMedix.Server.Controllers.Admin;

/// <inheritdoc />
[ApiController]
[Authorize(Roles = "Admin")]
[Route("[controller]/[action]")]
public class AdminController(
    IMapper mapper,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext db) : ControllerBase
{
    /// <summary>
    ///     This method is responsible for getting a list of all clinics.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAllClinics action.
    ///     If the operation is successful, it returns an OkResult with the list of clinics.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "A list of clinics", typeof(IEnumerable<ClinicModel>))]
    public IActionResult GetAllClinicsAsync()
    {
        var clinics = db.Clinics.ToList();
        var clinicsResponse = mapper.Map<IEnumerable<ClinicModel>>(clinics);

        return Ok(clinicsResponse);
    }

    /// <summary>
    ///     This method is responsible for getting a list of all data reports.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAllDataReports action.
    ///     If the operation is successful, it returns an OkResult with the list of data reports.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "A list of data reports", typeof(IEnumerable<DataReportResponse>))]
    public IActionResult GetAllDataReportsAsync()
    {
        var reports = db.DataReports.ToList();
        var reportsResponse = mapper.Map<IEnumerable<DataReportResponse>>(reports);

        return Ok(reportsResponse);
    }

    /// <summary>
    ///     This method is responsible for getting a list of all images.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAllImages action.
    ///     If the operation is successful, it returns an OkResult with the list of images.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "A list of images", typeof(IEnumerable<ImageResponse>))]
    public IActionResult GetAllImagesAsync()
    {
        var images = db.Images.ToList();
        var imagesResponse = mapper.Map<IEnumerable<ImageResponse>>(images);

        return Ok(imagesResponse);
    }

    /// <summary>
    ///     This method is responsible for getting a list of all roles.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAllRoles action.
    ///     If the operation is successful, it returns an OkResult with the list of roles.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "A list of roles", typeof(IEnumerable<IdentityRole>))]
    public IActionResult GetAllRolesAsync()
    {
        var roles = roleManager.Roles.ToList();
        return Ok(roles);
    }

    /// <summary>
    ///     This method is responsible for getting a list of all users.
    /// </summary>
    /// <returns>
    ///     An IActionResult that represents the result of the GetAllUsers action.
    ///     If the operation is successful, it returns an OkResult with the list of users.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "A list of users", typeof(IEnumerable<UserModel>))]
    public IActionResult GetAllUsersAsync()
    {
        var users = userManager.Users.ToList();
        var usersResponse = mapper.Map<IEnumerable<UserModel>>(users);

        return Ok(usersResponse);
    }

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
    [HttpPost]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> AddUserToAdminRoleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for adding a user to the Admin role by their email.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the AddUserToAdminRoleByEmail action.
    ///     If the user is successfully added to the Admin role, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> AddUserToAdminRoleByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return NotFound();

        var result = await userManager.AddToRoleAsync(user, "Admin");
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

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
    [SwaggerResponse(Status200OK, "The clinic was created successfully", typeof(CreateClinicResponse))]
    [SwaggerResponse(Status400BadRequest, "A clinic with the same name already exists", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateClinicAsync(ClinicModel clinic)
    {
        if (db.Clinics.Any(c => c.Name == clinic.Name))
            return BadRequest("A clinic with the same name already exists.");

        var newClinic = mapper.Map<Models.Clinic>(clinic);

        db.Clinics.Add(newClinic);
        await db.SaveChangesAsync();

        var response = new CreateClinicResponse
        {
            Message = "Clinic created successfully.",
            Clinic = clinic
        };

        return Ok(response);
    }

    /// <summary>
    ///     This method is responsible for creating a new role.
    /// </summary>
    /// <param name="roleName">The name of the role to create.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the CreateRole action.
    ///     If the role is successfully created, it returns an OkResult with a success message.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPost]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    public async Task<IActionResult> CreateRoleAsync(string roleName)
    {
        var roleExists = await roleManager.RoleExistsAsync(roleName);
        if (roleExists) return BadRequest("Role already exists");

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for deleting a specific role.
    /// </summary>
    /// <param name="roleId">The Id of the role to delete.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the DeleteRole action.
    ///     If the role is successfully deleted, it returns an OkResult with a success message.
    ///     If the role is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The role was not found", typeof(BaseResponse))]
    public async Task<IActionResult> DeleteRoleAsync(string roleId)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return NotFound();

        var result = await roleManager.DeleteAsync(role);
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for deleting a user.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the DeleteUser action.
    ///     If the user is successfully deleted, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.IsDeleted = true;
        await db.SaveChangesAsync();

        var result = await userManager.SetLockoutEnabledAsync(user, true);
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for deleting a user by their email.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the DeleteUserByEmail action.
    ///     If the user is successfully deleted, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> DeleteUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return NotFound();

        user.IsDeleted = true;
        await db.SaveChangesAsync();

        var result = await userManager.SetLockoutEnabledAsync(user, true);
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for getting details of a specific user by their email.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the GetUserByEmail action.
    ///     If the user is found, it returns an OkResult with the user details.
    ///     If the user is not found, it returns a NotFoundResult.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "The user details", typeof(ApplicationUser))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    ///     This method is responsible for getting details of a specific user by their ID.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the GetUserById action.
    ///     If the user is found, it returns an OkResult with the user details.
    ///     If the user is not found, it returns a NotFoundResult.
    /// </returns>
    [HttpGet]
    [SwaggerResponse(Status200OK, "The user details", typeof(ApplicationUser))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> GetUserByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        return Ok(user);
    }

    /// <summary>
    ///     Retrieves the reports associated with a specific user based on the provided user id.
    /// </summary>
    /// <param name="userId">The id of the user whose reports to retrieve.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - Ok with the list of reports if the reports are successfully retrieved.
    ///     - NotFound if the user is not found.
    /// </returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerResponse(Status200OK, "A list of reports associated with the user", typeof(IEnumerable<DataReportResponse>))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> GetUserReportsAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var reports = db.DataReports.Where(r => r.User.Id == userId).ToList();
        var reportsResponse = mapper.Map<IEnumerable<DataReportResponse>>(reports);

        return Ok(reportsResponse);
    }

    /// <summary>
    ///     Retrieves the reports associated with a specific user based on the provided email.
    /// </summary>
    /// <param name="email">The email of the user whose reports to retrieve.</param>
    /// <returns>
    ///     Returns an IActionResult:
    ///     - Ok with the list of reports if the reports are successfully retrieved.
    ///     - NotFound if the user is not found.
    /// </returns>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [SwaggerResponse(Status200OK, "A list of reports associated with the user", typeof(IEnumerable<DataReportResponse>))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> GetUserReportsByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return NotFound();

        var reports = db.DataReports.Where(r => r.User.Email == email).ToList();
        var reportsResponse = mapper.Map<IEnumerable<DataReportResponse>>(reports);

        return Ok(reportsResponse);
    }

    /// <summary>
    ///     This method is responsible for removing a user from the Admin role.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the RemoveUserFromAdminRole action.
    ///     If the user is successfully removed from the Admin role, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> RemoveUserFromAdminRoleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.RemoveFromRoleAsync(user, "Admin");
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for removing a user from the Admin role by their email.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the RemoveUserFromAdminRoleByEmail action.
    ///     If the user is successfully removed from the Admin role, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpDelete]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> RemoveUserFromAdminRoleByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null) return NotFound();

        var result = await userManager.RemoveFromRoleAsync(user, "Admin");
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }

    /// <summary>
    ///     This method is responsible for updating details of a specific user.
    /// </summary>
    /// <param name="userId">The Id of the user.</param>
    /// <param name="user">The user details to update.</param>
    /// <returns>
    ///     An IActionResult that represents the result of the UpdateUser action.
    ///     If the user is successfully updated, it returns an OkResult with a success message.
    ///     If the user is not found, it returns a NotFoundResult.
    ///     If the operation fails, it returns a BadRequestObjectResult with the errors.
    /// </returns>
    [HttpPut]
    [SwaggerResponse(Status200OK, "A success message", typeof(BaseResponse))]
    [SwaggerResponse(Status400BadRequest, "The operation failed", typeof(ErrorResponse))]
    [SwaggerResponse(Status404NotFound, "The user was not found", typeof(BaseResponse))]
    public async Task<IActionResult> UpdateUserAsync(string userId, AdminUserEditRequest user)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser is null) return NotFound();

        existingUser.UserName = user.Email ?? existingUser.UserName;
        existingUser.Email = user.Email ?? existingUser.Email;
        existingUser.IsClinicAdmin = user.IsClinicAdmin ?? existingUser.IsClinicAdmin;
        existingUser.IsDeleted = user.IsDeleted ?? existingUser.IsDeleted;

        var result = await userManager.UpdateAsync(existingUser);
        if (result.Succeeded) return Ok();

        return BadRequest(result.Errors);
    }
}