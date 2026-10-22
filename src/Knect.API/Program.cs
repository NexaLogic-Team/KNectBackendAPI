using System.Text;
using Knect.API.Authorization;
using Knect.Application.Admin.Interfaces;
using Knect.Application.Applications.Interfaces;
using Knect.Application.Authentication;
using Knect.Application.Authentication.Interfaces;
using Knect.Application.FileStorage;
using Knect.Application.Jobs.Interfaces;
using Knect.Application.Profiles.Interfaces;
using Knect.Application.Subscriptions.Interfaces;
using Knect.Infrastructure.Admin;
using Knect.Infrastructure.Applications;
using Knect.Infrastructure.Authentication;
using Knect.Infrastructure.BackgroundJobs;
using Knect.Infrastructure.Development;
using Knect.Infrastructure.FileStorage;
using Knect.Infrastructure.Jobs;
using Knect.Infrastructure.Notifications;
using Knect.Infrastructure.Persistence;
using Knect.Infrastructure.Profiles;
using Knect.Infrastructure.Subscriptions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore; // <-- Add this import

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<JwtOptions>().Bind(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddOptions<GoogleAuthOptions>().Bind(builder.Configuration.GetSection(GoogleAuthOptions.SectionName));
builder.Services.AddOptions<EmailOptions>().Bind(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddOptions<S3StorageOptions>().Bind(builder.Configuration.GetSection(S3StorageOptions.SectionName));
builder.Services.AddOptions<SubscriptionLifecycleOptions>().Bind(builder.Configuration.GetSection(SubscriptionLifecycleOptions.SectionName));
builder.Services.AddOptions<DevelopmentAdminSeedOptions>().Bind(builder.Configuration.GetSection(DevelopmentAdminSeedOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required. Set it via environment variables or secrets before startup.");
}

builder.Services.AddDbContext<KnectDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
    });
});

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Contains("replace", StringComparison.OrdinalIgnoreCase) || jwtKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be configured with a secure value of at least 32 characters before starting the API.");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "https://api.example.com";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "knect-api";

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IGoogleAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IEmployerProfileService, EmployerProfileService>();
builder.Services.AddScoped<IJobCategoryService, JobCategoryService>();
builder.Services.AddScoped<IJobExperienceService, JobExperienceService>();
builder.Services.AddScoped<IJobService, JobService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
builder.Services.AddScoped<IEmployerSubscriptionService, EmployerSubscriptionService>();
builder.Services.AddScoped<IPaymentReviewService, PaymentReviewService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IAdminEmployerService, AdminEmployerService>();
builder.Services.AddScoped<IAdminAuditService, AdminAuditService>();
builder.Services.AddScoped<IAdminReportService, AdminReportService>();
builder.Services.AddScoped<PlatformNotificationService>();
builder.Services.AddScoped<SubscriptionLifecycleService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<DevelopmentAdminSeeder>();
}

builder.Services.AddHostedService<SubscriptionExpiryBackgroundService>();

if (builder.Environment.IsProduction())
{
    builder.Services.AddSingleton<IFileStorageService, S3FileStorageService>();
}
else
{
    builder.Services.AddSingleton<IFileStorageService, MemoryFileStorageService>();
}

builder.Services.AddScoped<IAuthorizationHandler, CurrentSessionAuthorizationHandler>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
        policy.Requirements.Add(new CurrentSessionRequirement());
    });
    options.AddPolicy("EmployerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Employer");
    });
    options.AddPolicy("SubscriptionAdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Accessible at: http://localhost:<port>/scalar/v1
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", async (KnectDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        await dbContext.Database.CanConnectAsync(cancellationToken);
        return Results.Ok(new { status = "ready" });
    }
    catch
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();
