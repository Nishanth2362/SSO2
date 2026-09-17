using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using SSO.Application.Configuaration;
using SSO.Application.Extensions;
using SSO.Application.Interfaces.Services;
using SSO.Application.Interfaces.Services.Features;
using SSO.Domain.Entities;
using SSO.Infrastructure.Contexts;
using SSO.Infrastructure.Extentions;
using SSO.Infrastructure.Services;
using SSO.Infrastructure.Services.Features;
using SSO.InternalApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database (reads DbSettings from appsettings.json) ────────────────────
var provider = builder.Configuration["DbSettings:Provider"]?.ToLower();
var connStr  = builder.Configuration["DbSettings:DefaultConnection"]
               ?? throw new InvalidOperationException("DbSettings:DefaultConnection not set.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if      (provider == "mysql")      options.UseMySQL(connStr);
    else if (provider == "postgresql") options.UseNpgsql(connStr);
    else                               options.UseSqlServer(connStr);
});

// ── ASP.NET Identity ──────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ── Shared Infrastructure & Application Services ──────────────────────────
builder.Services.AddLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJobProgressService, NullJobProgressService>();

// Register DateTime, Mail, Audit, Upload, EmailTemplate services
builder.Services.AddTransient<IDateTimeService, SystemDateTimeService>();
builder.Services.Configure<MailConfiguration>(builder.Configuration.GetSection("MailConfiguration"));
builder.Services.AddTransient<IMailService, SMTPMailService>();

builder.Services.AddTransient<IUploadService, UploadService>();
builder.Services.AddTransient<IAuditService, AuditService>();
builder.Services.AddTransient<IEmailTemplateService, EmailTemplateService>();

// Register Application & Repository layers
builder.Services.AddApplicationLayer();
builder.Services.AddRepositories();

// Register OpenIddict core services
builder.Services.AddInfrastructureExtentions(builder.Configuration);

builder.Services.AddControllers();

// ── Swagger / OpenAPI Config ─────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Schoola SSO - Internal API",
        Version = "v1",
        Description = "Internal Gateway API for Schoola SSO Applications"
    });
});

var app = builder.Build();

// Enable Swagger UI at /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Internal API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.MapControllers();
app.Run();
