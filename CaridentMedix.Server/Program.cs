using System.Reflection;
using System.Text;
using AutoMapper;
using AutoMapper.EquivalencyExpression;
using CaridentMedix.Server.Controllers.Admin;
using CaridentMedix.Server.Controllers.Appointment;
using CaridentMedix.Server.Controllers.Clinic;
using CaridentMedix.Server.Controllers.Image;
using CaridentMedix.Server.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Serilog;

Log.Logger = new LoggerConfiguration()
   .WriteTo.Console()
   .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    builder.Services.AddSerilog((_, lc) => lc
       .WriteTo.Console()
       .ReadFrom.Configuration(configuration));

    var conStrBuilder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection"))
    {
        Password = configuration["DbPassword"]
    };

    builder.Services
       .AddDbContext<ApplicationDbContext>(options => options
           .UseNpgsql(conStrBuilder.ConnectionString)
           .UseLazyLoadingProxies())
       .AddIdentity<ApplicationUser, IdentityRole>()
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    builder.Services.AddAutoMapper((serviceProvider, automapper) =>
    {
        automapper.AddCollectionMappers();
        automapper.UseEntityFrameworkCoreModel<ApplicationDbContext>(serviceProvider);

        automapper.CreateMap<ApplicationUser, GetSelfResponse>().ReverseMap();
        automapper.CreateMap<ApplicationUser, AdminModel>().ReverseMap();
        automapper.CreateMap<ApplicationUser, AppointmentUserModel>().ReverseMap();
        automapper.CreateMap<ApplicationUser, DataReportUserModel>().ReverseMap();
        automapper.CreateMap<Clinic, ClinicModel>().ReverseMap();
        automapper.CreateMap<Dentist, DentistModel>().ReverseMap();
        automapper.CreateMap<DataReport, DataReportRequest>().ReverseMap();
        automapper.CreateMap<DataReport, DataReportModel>().ReverseMap();
        automapper.CreateMap<DataReport, DataReportModel>().ReverseMap();
        automapper.CreateMap<Image, ImageModel>().ReverseMap();
        automapper.CreateMap<Appointment, AppointmentModel>().ReverseMap();
    }, typeof(ApplicationDbContext).Assembly);

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = configuration["Swagger:Version"],
            Title = configuration["Swagger:Title"],
            Description = configuration["Swagger:Description"],
            License = new OpenApiLicense
            {
                Name = configuration["Swagger:License:Name"],
                Url = new Uri(configuration["Swagger:License:Url"]!)
            },
            Contact = new OpenApiContact
            {
                Name = configuration["Swagger:Contact:Name"],
                Url = new Uri(configuration["Swagger:Contact:Url"]!)
            },
            TermsOfService = new Uri(configuration["Swagger:TermsOfService"]!)
        });

        foreach (var server in configuration.GetSection("Swagger:Servers").GetChildren())
        {
            options.AddServer(new OpenApiServer
            {
                Url = server["Url"],
                Description = server["Description"]
            });
        }

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                []
            }
        });

        var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xml));
    });

    builder.Services
       .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
       .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
            };
        });

    builder.Services.AddCors(options => options
       .AddPolicy("AllowAll", x => x
           .AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader()));

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        if (args.Contains("--reset-db"))
            db.Database.EnsureDeleted();

        db.Database.EnsureCreated();

        // Create an admin user if one does not exist
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(configuration["Admin:Email"]!);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Email = configuration["Admin:Email"],
                UserName = configuration["Admin:Email"]
            };

            await userManager.CreateAsync(adminUser, configuration["Admin:Password"]!);

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            var result = await userManager.AddToRoleAsync(adminUser, "Admin");
            if (result.Succeeded)
                Log.Information("Admin user created successfully");
            else
                Log.Error("Failed to create admin user: {@Errors}", result.Errors);
        }

        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRewriter(new RewriteOptions().AddRedirect("^$", "swagger"));

    app.MapControllers();
    app.UseStaticFiles();

    app.UseCors("AllowAll");
    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while starting the application");
    throw;
}
finally
{
    Log.CloseAndFlush();
}