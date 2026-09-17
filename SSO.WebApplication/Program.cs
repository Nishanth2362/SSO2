using Asp.Versioning;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Localization;
using MySql.EntityFrameworkCore.Extensions;
using SSO.Application.Extensions;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Extentions;
using SSO.Infrastructure.Models;
using SSO.WebApplication.Managers.Preferences;
using SSO.WebApplication.Middlewares;
using SSO.WebApplication.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrelHttpsConfiguration();
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddForwarding(builder.Configuration);
builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});
builder.Services.AddCurrentUserService();
builder.Services.AddSecurityHardening();
builder.Services.AddSerialization();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentity();
builder.Services.AddServerStorage(); //TODO - should implement ServerStorageProvider to work correctly!
builder.Services.AddScoped<ServerPreferenceManager>();
builder.Services.AddServerLocalization();
//builder.Services.AddIdentity();
builder.Services.AddJwtAuthentication(builder.Services.GetApplicationSettings(builder.Configuration));
builder.Services.AddSignalR();
builder.Services.AddApplicationServices();
builder.Services.AddApplicationLayer();
builder.Services.AddRepositories();
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.RegisterSwagger();
builder.Services.AddInfrastructureExtentions(builder.Configuration);
#pragma warning disable CS0612 // Type or member is obsolete
builder.Services.ConfigureHangefire(builder.Configuration);
#pragma warning restore CS0612 // Type or member is obsolete
builder.Services.AddControllers();
builder.Services.AddAntiforgery(options =>
{
    // options.HeaderName = "X-XSRF-TOKEN"; // Only needed for AJAX with custom header
    options.Cookie.Name = ".SSO.Antiforgery";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});


//services.AddRazorPages();
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
});
builder.Services.AddLazyCache();
builder.Services.AddControllersWithViews().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Host.UseSerilog(); 

var app = builder.Build();

app.MapDefaultEndpoints();

IStringLocalizer<Program> localizer;
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Multi-DB migration support
        if (app.Environment.IsDevelopment() && context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
           // context.Database.Migrate();
        }
        localizer = services.GetRequiredService<IStringLocalizer<Program>>();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
app.UseForwarding(app.Configuration);
app.UseExceptionHandling(app.Environment);

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"Files")),
    RequestPath = new PathString("/Files"),
    ServeUnknownFileTypes = false
});

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseRequestLocalizationByCulture();

app.UseRouting();
app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseHangfireDashboard("/backgroundJobs", new DashboardOptions
{
    AppPath = null,
    DarkModeEnabled = false,
    DashboardTitle = "SSO Job Monitor",
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.UseEndpoints();
app.ConfigureSwagger(app.Environment, app.Configuration);
await app.Initialize(app.Configuration);
app.MapControllers();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}");


app.Run();
